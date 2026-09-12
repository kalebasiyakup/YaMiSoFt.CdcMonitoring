using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class AlertEventRepository(CdcMonitoringDbContext db) : IAlertEventRepository
{
    public Task<AlertEvent?> GetActiveAsync(AlertType type, Guid? connectionId, Guid? relationshipId, CancellationToken ct = default) =>
        db.AlertEvents.FirstOrDefaultAsync(a =>
            a.Type == type &&
            a.ConnectionId == connectionId &&
            a.RelationshipId == relationshipId &&
            a.ResolvedAt == null, ct);

    public Task<List<AlertEvent>> GetRecentAsync(int take, CancellationToken ct = default) =>
        db.AlertEvents.OrderByDescending(a => a.TriggeredAt).Take(take).ToListAsync(ct);

    public async Task AddAsync(AlertEvent alertEvent, CancellationToken ct = default) =>
        await db.AlertEvents.AddAsync(alertEvent, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
