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

        foreach (var r in trusted)
        {
            try
            {
                var result = await ReconcileOneAsync(r, ct);
                await results.AddAsync(result, ct);
                await results.SaveChangesAsync(ct);

                if (!result.IsMatch)
                {
                    var sourceName = r.SourceConnection?.Name ?? r.SourceConnectionId.ToString();
                    var targetName = r.TargetConnection?.Name ?? r.TargetConnectionId.ToString();
                    mismatchSummaries.Add($"{sourceName} -> {targetName} ({r.SlotName}): {result.Details}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reconciliation başarısız: ilişki {RelationshipId} ({SlotName})", r.Id, r.SlotName);
            }
        }

        if (mismatchSummaries.Count > 0)
        {
            var body = "Haftalık reconciliation'da aşağıdaki ilişkilerde tutarsızlık bulundu:\n\n" +
                       string.Join("\n\n", mismatchSummaries);

            await emailNotifier.SendReportAsync(
                "CDC Monitoring — Haftalık Reconciliation Raporu (tutarsızlık bulundu)", body, ct);
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
            var sourceStats = await inspector.GetTableChecksumAsync(source, sourcePassword, table.SchemaName, table.TableName, ct);
            var targetStats = await inspector.GetTableChecksumAsync(target, targetPassword, table.SchemaName, table.TableName, ct);

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

    private static string CombineHash(IEnumerable<string> parts)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", parts)));
        return Convert.ToHexString(bytes);
    }
}
