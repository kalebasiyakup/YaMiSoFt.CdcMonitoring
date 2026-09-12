using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class InMemoryCdcRelationshipRepository : ICdcRelationshipRepository
{
    private readonly List<CdcRelationship> _items = [];

    public Task<List<CdcRelationship>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(_items.ToList());

    public Task<CdcRelationship?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(r => r.Id == id));

    public Task<CdcRelationship?> FindAsync(Guid sourceConnectionId, Guid targetConnectionId, string slotName, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(r =>
            r.SourceConnectionId == sourceConnectionId &&
            r.TargetConnectionId == targetConnectionId &&
            r.SlotName == slotName));

    public Task AddAsync(CdcRelationship relationship, CancellationToken ct = default)
    {
        _items.Add(relationship);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
