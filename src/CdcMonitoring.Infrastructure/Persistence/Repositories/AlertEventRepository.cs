using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Alerting;
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

    public async Task<(List<AlertEvent> Items, int TotalCount)> GetPagedAsync(
        AlertEventFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.AlertEvents.AsQueryable();

        if (filter.Type is { } type)
            query = query.Where(a => a.Type == type);
        if (filter.Severity is { } severity)
            query = query.Where(a => a.Severity == severity);
        if (filter.OnlyActive is { } onlyActive)
            query = query.Where(a => onlyActive ? a.ResolvedAt == null : a.ResolvedAt != null);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(a => a.TriggeredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public async Task AddAsync(AlertEvent alertEvent, CancellationToken ct = default) =>
        await db.AlertEvents.AddAsync(alertEvent, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
