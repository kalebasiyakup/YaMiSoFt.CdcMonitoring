using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class CdcRelationshipRepository(CdcMonitoringDbContext db) : ICdcRelationshipRepository
{
    public Task<List<CdcRelationship>> GetAllAsync(CancellationToken ct = default) =>
        db.CdcRelationships
            .Include(r => r.SourceConnection)
            .Include(r => r.TargetConnection)
            .ToListAsync(ct);

    public Task<CdcRelationship?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.CdcRelationships
            .Include(r => r.SourceConnection)
            .Include(r => r.TargetConnection)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<CdcRelationship?> FindAsync(Guid sourceConnectionId, Guid targetConnectionId, string slotName, CancellationToken ct = default) =>
        db.CdcRelationships.FirstOrDefaultAsync(r =>
            r.SourceConnectionId == sourceConnectionId &&
            r.TargetConnectionId == targetConnectionId &&
            r.SlotName == slotName, ct);

    public async Task AddAsync(CdcRelationship relationship, CancellationToken ct = default) =>
        await db.CdcRelationships.AddAsync(relationship, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
