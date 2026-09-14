using System.Net;
using System.Security.Cryptography;
using System.Text;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CdcMonitoring.Application.Reconciliation;

/// <summary>
/// Kaynak-hedef reconciliation (FR-11), sıklığı `/settings`'ten dakika/saat/gün olarak
/// seçilebilir. Yalnızca kullanıcı tarafından onaylanmış (Confirmed/Manual) ilişkiler için
/// çalışır — keşfedilmiş ama henüz onaylanmamış (Inferred) ilişkiler bir öneriden ibarettir
/// (bkz. FR-06). Tüm sorgular salt-okumadır (FR-14).
/// </summary>
public class ReconciliationService(
    ICdcRelationshipRepository relationships,
    IReconciliationResultRepository results,
    IAlertEventRepository alertEvents,
    IConnectionPasswordProtector passwordProtector,
    IPostgresInspector inspector,
    IEmailNotifier emailNotifier,
    ISystemSettingsRepository settingsRepository,
    IClock clock,
    ILogger<ReconciliationService> logger)
{
    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        var settings = await settingsRepository.GetAsync(ct);
        var all = await relationships.GetAllAsync(ct);
        var trusted = all.Where(r => r.Status is CdcRelationshipStatus.Confirmed or CdcRelationshipStatus.Manual).ToList();

        var mismatchRows = new List<ReportRow>();
        var failureRows = new List<ReportRow>();

        foreach (var r in trusted)
        {
            var sourceName = r.SourceConnection?.Name ?? r.SourceConnectionId.ToString();
            var targetName = r.TargetConnection?.Name ?? r.TargetConnectionId.ToString();

            try
            {
                var result = await ReconcileOneAsync(r, ct);
                await results.AddAsync(result, ct);
                await results.SaveChangesAsync(ct);

                if (!result.IsMatch)
                    mismatchRows.Add(new ReportRow(sourceName, targetName, r.SlotName, result.Details ?? ""));

                await UpdateMismatchAlertAsync(r, conditionActive: !result.IsMatch,
                    () => $"{sourceName} -> {targetName}: veri tutarlılık kontrolü tutarsız ({r.SlotName}) — {result.Details}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Veri tutarlılık kontrolü başarısız: ilişki {RelationshipId} ({SlotName})", r.Id, r.SlotName);
                // Hata da mismatch gibi rapora dahil edilir: aksi halde her ilişki hata verdiğinde
                // (ör. genel bir bağlantı kesintisinde) mismatchRows boş kalır ve FR-11'in tek
                // çıktısı olan e-posta hiç gönderilmez — operatörler kontrolün tamamen başarısız
                // olduğundan habersiz kalır.
                failureRows.Add(new ReportRow(sourceName, targetName, r.SlotName, ex.Message));

                await UpdateMismatchAlertAsync(r, conditionActive: true,
                    () => $"{sourceName} -> {targetName}: veri tutarlılık kontrolü çalıştırılamadı ({r.SlotName}) — {ex.Message}", ct);
            }
        }

        if (mismatchRows.Count > 0 || failureRows.Count > 0)
        {
            var html = BuildReportHtml(mismatchRows, failureRows, clock.UtcNow, settings.ReconciliationIntervalSeconds);
            await emailNotifier.SendReportAsync(
                "CDC Monitoring — Veri Tutarlılık Kontrolü Raporu (tutarsızlık/hata bulundu)", html, ct);
        }
    }

    private async Task<ReconciliationResult> ReconcileOneAsync(CdcRelationship r, CancellationToken ct)
    {
        var source = r.SourceConnection ?? throw new InvalidOperationException("Kaynak bağlantı yüklenmedi.");
        var target = r.TargetConnection ?? throw new InvalidOperationException("Hedef bağlantı yüklenmedi.");

        var sourcePassword = passwordProtector.Unprotect(source.EncryptedPassword);
        var targetPassword = passwordProtector.Unprotect(target.EncryptedPassword);

        var publicationNames = r.PublicationName
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var tables = await inspector.GetPublicationTablesAsync(source, sourcePassword, publicationNames, ct);

        long totalSourceRows = 0, totalTargetRows = 0;
        var sourceChecksums = new List<string>();
        var targetChecksums = new List<string>();
        var details = new List<string>();
        var allMatch = true;

        foreach (var table in tables)
        {
            // Kolon listesi kaynaktan alınır ve hem kaynak hem hedef sorgusuna aynen geçirilir:
            // hedef tarafın kendi eklediği fazladan bir kolon (ör. bookkeeping alanı) bu sayede
            // checksum'ı asla etkilemez — yalnızca gerçekten replike edilen kolonlar karşılaştırılır.
            var columns = await inspector.GetTableColumnsAsync(source, sourcePassword, table.SchemaName, table.TableName, ct);
            var columnNames = columns.Select(c => c.Name).ToList();
            var sourceStats = await inspector.GetTableChecksumAsync(source, sourcePassword, table.SchemaName, table.TableName, columnNames, ct);
            var targetStats = await inspector.GetTableChecksumAsync(target, targetPassword, table.SchemaName, table.TableName, columnNames, ct);

            totalSourceRows += sourceStats.RowCount;
            totalTargetRows += targetStats.RowCount;
            sourceChecksums.Add(sourceStats.Checksum);
            targetChecksums.Add(targetStats.Checksum);

            var tableMatch = sourceStats.RowCount == targetStats.RowCount && sourceStats.Checksum == targetStats.Checksum;
            allMatch &= tableMatch;
            details.Add($"{table.SchemaName}.{table.TableName}: kaynak={sourceStats.RowCount} satır, hedef={targetStats.RowCount} satır, {(tableMatch ? "eşleşti" : "TUTARSIZ")}");
        }

        if (tables.Count == 0)
        {
            details.Add("Publication'da izlenebilir tablo bulunamadı.");
            allMatch = false;
        }

        return new ReconciliationResult
        {
            Id = Guid.NewGuid(),
            RelationshipId = r.Id,
            RunAt = clock.UtcNow,
            SourceRowCount = totalSourceRows,
            TargetRowCount = totalTargetRows,
            SourceChecksum = CombineHash(sourceChecksums),
            TargetChecksum = CombineHash(targetChecksums),
            IsMatch = allMatch,
            Details = string.Join("; ", details)
        };
    }

    // AlertEvaluationService.EvaluateRuleAsync ile aynı tetikle/kapat mantığı, ancak e-posta
    // bildirimi burada tekrar gönderilmez — RunOnceAsync sonunda tüm tutarsızlıklar zaten tek
    // bir toplu rapor e-postasıyla bildiriliyor (mevcut yol korunuyor).
    private async Task UpdateMismatchAlertAsync(
        CdcRelationship r, bool conditionActive, Func<string> messageFactory, CancellationToken ct)
    {
        var active = await alertEvents.GetActiveAsync(AlertType.ReconciliationMismatch, connectionId: null, r.Id, ct);

        if (!conditionActive)
        {
            if (active is not null)
            {
                active.ResolvedAt = clock.UtcNow;
                await alertEvents.SaveChangesAsync(ct);
            }
            return;
        }

        if (active is null)
        {
            var now = clock.UtcNow;
            active = new AlertEvent
            {
                Id = Guid.NewGuid(),
                Type = AlertType.ReconciliationMismatch,
                Severity = AlertSeverity.Warning,
                RelationshipId = r.Id,
                TriggeredAt = now,
                NotifiedAt = now,
                Message = messageFactory()
            };
            await alertEvents.AddAsync(active, ct);
            await alertEvents.SaveChangesAsync(ct);
        }
    }

    private static string CombineHash(IEnumerable<string> parts)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", parts)));
        return Convert.ToHexString(bytes);
    }

    private readonly record struct ReportRow(string SourceName, string TargetName, string SlotName, string Detail);

    private static string BuildReportHtml(
        IReadOnlyList<ReportRow> mismatchRows, IReadOnlyList<ReportRow> failureRows,
        DateTimeOffset runAt, int reconciliationIntervalSeconds)
    {
        var sb = new StringBuilder();

        sb.Append("""
            <div style="margin:0;padding:24px;background:#f4f5f7;font-family:'Segoe UI',Arial,sans-serif;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:640px;margin:0 auto;background:#ffffff;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;">
                <tr>
                  <td style="padding:20px 24px;background:#b42318;">
                    <div style="color:#ffffff;font-size:18px;font-weight:600;">CDC Monitoring</div>
                    <div style="color:#fecaca;font-size:13px;margin-top:2px;">Veri Tutarlılık Kontrolü Raporu</div>
                  </td>
                </tr>
                <tr>
                  <td style="padding:20px 24px 4px;">
            """);

        sb.Append("<p style=\"margin:0 0 4px;font-size:14px;color:#111827;\">")
            .Append($"<strong>{mismatchRows.Count}</strong> ilişkide tutarsızlık, <strong>{failureRows.Count}</strong> ilişkide kontrol hatası bulundu.")
            .Append("</p>");

        sb.Append("<p style=\"margin:0 0 20px;font-size:12px;color:#6b7280;\">")
            .Append($"Çalıştırma zamanı: {runAt:yyyy-MM-dd HH:mm} UTC &middot; Kontrol sıklığı: her {FormatInterval(reconciliationIntervalSeconds)}")
            .Append("</p>");

        AppendSection(sb, "Tutarsızlık Bulunan İlişkiler", mismatchRows, "#b45309", "#fffbeb");
        AppendSection(sb, "Kontrol Edilemeyen İlişkiler (Hata)", failureRows, "#b91c1c", "#fef2f2");

        sb.Append("""
                  </td>
                </tr>
                <tr>
                  <td style="padding:16px 24px;background:#f9fafb;border-top:1px solid #e5e7eb;">
                    <p style="margin:0;font-size:11px;color:#9ca3af;">
                      Bu otomatik bir bildirimdir — CDC Monitoring hiçbir koşulda otomatik düzeltici aksiyon almaz.
                      Detaylar için Veri Tutarlılık Kontrolü ekranını inceleyin.
                    </p>
                  </td>
                </tr>
              </table>
            </div>
            """);

        return sb.ToString();
    }

    private static void AppendSection(
        StringBuilder sb, string title, IReadOnlyList<ReportRow> rows, string accentColor, string headerBg)
    {
        if (rows.Count == 0)
            return;

        sb.Append($"<h3 style=\"margin:0 0 8px;font-size:13px;color:{accentColor};text-transform:uppercase;letter-spacing:.03em;\">")
            .Append(WebUtility.HtmlEncode(title))
            .Append("</h3>");

        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"margin:0 0 20px;border-collapse:collapse;font-size:12px;\">");
        sb.Append($"<tr style=\"background:{headerBg};\">")
            .Append("<th style=\"text-align:left;padding:6px 8px;border:1px solid #e5e7eb;color:#374151;\">İlişki</th>")
            .Append("<th style=\"text-align:left;padding:6px 8px;border:1px solid #e5e7eb;color:#374151;\">Slot</th>")
            .Append("<th style=\"text-align:left;padding:6px 8px;border:1px solid #e5e7eb;color:#374151;\">Detay</th>")
            .Append("</tr>");

        foreach (var row in rows)
        {
            sb.Append("<tr>")
                .Append("<td style=\"padding:6px 8px;border:1px solid #e5e7eb;color:#111827;white-space:nowrap;\">")
                .Append(WebUtility.HtmlEncode(row.SourceName)).Append(" &rarr; ").Append(WebUtility.HtmlEncode(row.TargetName))
                .Append("</td>")
                .Append("<td style=\"padding:6px 8px;border:1px solid #e5e7eb;color:#111827;white-space:nowrap;\">")
                .Append(WebUtility.HtmlEncode(row.SlotName))
                .Append("</td>")
                .Append("<td style=\"padding:6px 8px;border:1px solid #e5e7eb;color:#4b5563;\">")
                .Append(WebUtility.HtmlEncode(row.Detail))
                .Append("</td>")
                .Append("</tr>");
        }

        sb.Append("</table>");
    }

    private static string FormatInterval(int totalSeconds)
    {
        if (totalSeconds >= 86400 && totalSeconds % 86400 == 0)
        {
            var days = totalSeconds / 86400;
            return days == 1 ? "1 gün" : $"{days} gün";
        }

        if (totalSeconds >= 3600 && totalSeconds % 3600 == 0)
        {
            var hours = totalSeconds / 3600;
            return hours == 1 ? "1 saat" : $"{hours} saat";
        }

        var minutes = Math.Max(1, totalSeconds / 60);
        return minutes == 1 ? "1 dakika" : $"{minutes} dakika";
    }
}
