using CdcMonitoring.Application.Abstractions;

namespace CdcMonitoring.Application.Reconciliation;

public class ReconciliationQueryService(
    IReconciliationResultRepository results,
    ICdcRelationshipRepository relationships)
{
    public async Task<List<ReconciliationResultSummary>> GetRecentAsync(int take = 100, CancellationToken ct = default)
    {
        var recent = await results.GetRecentAsync(take, ct);
        var summaries = new List<ReconciliationResultSummary>(recent.Count);

        foreach (var r in recent)
        {
            var relationship = await relationships.GetByIdAsync(r.RelationshipId, ct);
            summaries.Add(new ReconciliationResultSummary(
                r.Id,
                relationship?.SourceConnection?.Name ?? "?",
                relationship?.TargetConnection?.Name ?? "?",
                relationship?.SlotName ?? "?",
                r.RunAt,
                r.SourceRowCount,
                r.TargetRowCount,
                r.IsMatch,
                r.Details));
        }

        return summaries;
    }
}
