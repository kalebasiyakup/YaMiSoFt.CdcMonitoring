using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class CdcRelationshipHealthRepository(CdcMonitoringDbContext db) : ICdcRelationshipHealthRepository
{
    public async Task AddAsync(CdcRelationshipHealth entry, CancellationToken ct = default) =>
        await db.CdcRelationshipHealthEntries.AddAsync(entry, ct);

    public Task<CdcRelationshipHealth?> GetLatestAsync(Guid relationshipId, CancellationToken ct = default) =>
        db.CdcRelationshipHealthEntries
            .Where(h => h.RelationshipId == relationshipId)
            .OrderByDescending(h => h.CheckedAt)
            .FirstOrDefaultAsync(ct);

    public Task<List<CdcRelationshipHealth>> GetSinceAsync(Guid relationshipId, DateTimeOffset since, CancellationToken ct = default) =>
        db.CdcRelationshipHealthEntries
            .Where(h => h.RelationshipId == relationshipId && h.CheckedAt >= since)
            .OrderByDescending(h => h.CheckedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default) =>
        db.CdcRelationshipHealthEntries.Where(h => h.CheckedAt < cutoff).ExecuteDeleteAsync(ct);
}
