using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class ConnectionHealthCheckRepository(CdcMonitoringDbContext db) : IConnectionHealthCheckRepository
{
    public async Task AddAsync(ConnectionHealthCheck result, CancellationToken ct = default) =>
        await db.ConnectionHealthChecks.AddAsync(result, ct);

    public Task<ConnectionHealthCheck?> GetLatestAsync(Guid connectionId, CancellationToken ct = default) =>
        db.ConnectionHealthChecks
            .Where(h => h.ConnectionId == connectionId)
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefaultAsync(ct);

    public Task<List<ConnectionHealthCheck>> GetRecentAsync(Guid connectionId, int take, CancellationToken ct = default) =>
        db.ConnectionHealthChecks
            .Where(h => h.ConnectionId == connectionId)
            .OrderByDescending(h => h.CheckedAt)
            .Take(take)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
