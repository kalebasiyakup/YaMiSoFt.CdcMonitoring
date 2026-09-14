using System.Security.Cryptography;
using System.Text;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CdcMonitoring.Application.Reconciliation;

/// <summary>
/// Haftalık kaynak-hedef reconciliation (FR-11). Yalnızca kullanıcı tarafından
/// onaylanmış (Confirmed/Manual) ilişkiler için çalışır — keşfedilmiş ama henüz
/// onaylanmamış (Inferred) ilişkiler bir öneriden ibarettir (bkz. FR-06).
/// Tüm sorgular salt-okumadır (FR-14).
/// </summary>
public class ReconciliationService(
    ICdcRelationshipRepository relationships,
    IReconciliationResultRepository results,
    IAlertEventRepository alertEvents,
    IConnectionPasswordProtector passwordProtector,
    IPostgresInspector inspector,
    IEmailNotifier emailNotifier,
    IClock clock,
    ILogger<ReconciliationService> logger)
{
    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        var all = await relationships.GetAllAsync(ct);
        var trusted = all.Where(r => r.Status is CdcRelationshipStatus.Confirmed or CdcRelationshipStatus.Manual).ToList();

        var mismatchSummaries = new List<string>();
        var failureSummaries = new List<string>();

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
                    mismatchSummaries.Add($"{sourceName} -> {targetName} ({r.SlotName}): {result.Details}");

                await UpdateMismatchAlertAsync(r, conditionActive: !result.IsMatch,
                    () => $"{sourceName} -> {targetName}: veri tutarlılık kontrolü tutarsız ({r.SlotName}) — {result.Details}", ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Veri tutarlılık kontrolü başarısız: ilişki {RelationshipId} ({SlotName})", r.Id, r.SlotName);
                // Hata da mismatch gibi rapora dahil edilir: aksi halde her ilişki hata verdiğinde
                // (ör. genel bir bağlantı kesintisinde) mismatchSummaries boş kalır ve FR-11'in tek
                // çıktısı olan e-posta hiç gönderilmez — operatörler kontrolün tamamen başarısız
                // olduğundan habersiz kalır.
                failureSummaries.Add($"{sourceName} -> {targetName} ({r.SlotName}): kontrol çalıştırılamadı — {ex.Message}");

                await UpdateMismatchAlertAsync(r, conditionActive: true,
                    () => $"{sourceName} -> {targetName}: veri tutarlılık kontrolü çalıştırılamadı ({r.SlotName}) — {ex.Message}", ct);
            }
        }

        if (mismatchSummaries.Count > 0 || failureSummaries.Count > 0)
        {
            var sections = new List<string>();
            if (mismatchSummaries.Count > 0)
                sections.Add("Tutarsızlık bulunan ilişkiler:\n\n" + string.Join("\n\n", mismatchSummaries));
            if (failureSummaries.Count > 0)
                sections.Add("Kontrol edilemeyen ilişkiler (hata):\n\n" + string.Join("\n\n", failureSummaries));

            var body = "Haftalık veri tutarlılık kontrolü sonucu:\n\n" + string.Join("\n\n", sections);

            await emailNotifier.SendReportAsync(
                "CDC Monitoring — Haftalık Veri Tutarlılık Kontrolü Raporu (tutarsızlık/hata bulundu)", body, ct);
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
}
