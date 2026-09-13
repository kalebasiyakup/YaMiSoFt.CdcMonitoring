using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Alerting;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class InMemoryAlertEventRepository : IAlertEventRepository
{
    private readonly List<AlertEvent> _items = [];

    public Task<AlertEvent?> GetActiveAsync(AlertType type, Guid? connectionId, Guid? relationshipId, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(a =>
            a.Type == type && a.ConnectionId == connectionId && a.RelationshipId == relationshipId && a.ResolvedAt is null));

    public Task<List<AlertEvent>> GetRecentAsync(int take, CancellationToken ct = default) =>
        Task.FromResult(_items.OrderByDescending(a => a.TriggeredAt).Take(take).ToList());

    public Task<(List<AlertEvent> Items, int TotalCount)> GetPagedAsync(
        AlertEventFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _items.AsEnumerable();

        if (filter.Type is { } type)
            query = query.Where(a => a.Type == type);
        if (filter.Severity is { } severity)
            query = query.Where(a => a.Severity == severity);
        if (filter.OnlyActive is { } onlyActive)
            query = query.Where(a => onlyActive ? a.ResolvedAt == null : a.ResolvedAt != null);

        var ordered = query.OrderByDescending(a => a.TriggeredAt).ToList();
        var paged = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult((paged, ordered.Count));
    }

    public Task AddAsync(AlertEvent alertEvent, CancellationToken ct = default)
    {
        _items.Add(alertEvent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
