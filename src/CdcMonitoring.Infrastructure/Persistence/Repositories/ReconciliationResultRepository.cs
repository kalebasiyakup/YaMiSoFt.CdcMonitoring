using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Reconciliation;
using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class ReconciliationResultRepository(CdcMonitoringDbContext db) : IReconciliationResultRepository
{
    public Task<List<ReconciliationResult>> GetRecentAsync(int take, CancellationToken ct = default) =>
        db.ReconciliationResults.OrderByDescending(r => r.RunAt).Take(take).ToListAsync(ct);

    public async Task AddAsync(ReconciliationResult result, CancellationToken ct = default) =>
        await db.ReconciliationResults.AddAsync(result, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default) =>
        db.ReconciliationResults.Where(r => r.RunAt < cutoff).ExecuteDeleteAsync(ct);

    public async Task<(List<ReconciliationResult> Items, int TotalCount)> GetPagedAsync(
        ReconciliationResultFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.ReconciliationResults.AsQueryable();

        if (filter.IsMatch is { } isMatch)
            query = query.Where(r => r.IsMatch == isMatch);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.RunAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }
}
