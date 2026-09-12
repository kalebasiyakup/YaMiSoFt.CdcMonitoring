using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class InMemoryConnectionHealthCheckRepository : IConnectionHealthCheckRepository
{
    public List<ConnectionHealthCheck> Results { get; } = [];

    public Task AddAsync(ConnectionHealthCheck result, CancellationToken ct = default)
    {
        Results.Add(result);
        return Task.CompletedTask;
    }

    public Task<ConnectionHealthCheck?> GetLatestAsync(Guid connectionId, CancellationToken ct = default) =>
        Task.FromResult(Results.Where(r => r.ConnectionId == connectionId)
            .OrderByDescending(r => r.CheckedAt)
            .FirstOrDefault());

    public Task<List<ConnectionHealthCheck>> GetRecentAsync(Guid connectionId, int take, CancellationToken ct = default) =>
        Task.FromResult(Results.Where(r => r.ConnectionId == connectionId)
            .OrderByDescending(r => r.CheckedAt)
            .Take(take)
            .ToList());

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
