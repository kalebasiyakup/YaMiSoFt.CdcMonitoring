using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class InMemoryReconciliationResultRepository : IReconciliationResultRepository
{
    public List<ReconciliationResult> Results { get; } = [];

    public Task<List<ReconciliationResult>> GetRecentAsync(int take, CancellationToken ct = default) =>
        Task.FromResult(Results.OrderByDescending(r => r.RunAt).Take(take).ToList());

    public Task AddAsync(ReconciliationResult result, CancellationToken ct = default)
    {
        Results.Add(result);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        var removed = Results.RemoveAll(r => r.RunAt < cutoff);
        return Task.FromResult(removed);
    }
}
