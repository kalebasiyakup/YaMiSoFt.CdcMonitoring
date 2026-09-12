using CdcMonitoring.Application.Abstractions;
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

    public Task AddAsync(AlertEvent alertEvent, CancellationToken ct = default)
    {
        _items.Add(alertEvent);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
