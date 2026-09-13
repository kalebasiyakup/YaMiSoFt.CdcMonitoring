using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Common;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Reconciliation;

public class ReconciliationQueryService(
    IReconciliationResultRepository results,
    ICdcRelationshipRepository relationships)
{
    public async Task<List<ReconciliationResultSummary>> GetRecentAsync(int take = 100, CancellationToken ct = default)
    {
        var recent = await results.GetRecentAsync(take, ct);
        return await ToSummariesAsync(recent, ct);
    }

    public async Task<PagedResult<ReconciliationResultSummary>> GetPagedAsync(
        ReconciliationResultFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await results.GetPagedAsync(filter, page, pageSize, ct);
        var summaries = await ToSummariesAsync(items, ct);
        return new PagedResult<ReconciliationResultSummary>(summaries, totalCount, page, pageSize);
    }

    private async Task<List<ReconciliationResultSummary>> ToSummariesAsync(List<ReconciliationResult> recent, CancellationToken ct)
    {
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
