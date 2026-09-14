using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.SchemaCatalog;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class InMemorySchemaChangeEventRepository : ISchemaChangeEventRepository
{
    public List<SchemaChangeEvent> Events { get; } = [];

    public Task AddRangeAsync(IEnumerable<SchemaChangeEvent> events, CancellationToken ct = default)
    {
        Events.AddRange(events);
        return Task.CompletedTask;
    }

    public Task<(List<SchemaChangeEvent> Items, int TotalCount)> GetPagedAsync(SchemaChangeFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var items = Events
            .Where(e => filter.ConnectionId is null || e.ConnectionId == filter.ConnectionId)
            .Where(e => filter.ChangeType is null || e.ChangeType == filter.ChangeType)
            .Where(e => filter.Since is null || e.DetectedAt >= filter.Since)
            .OrderByDescending(e => e.DetectedAt)
            .ToList();

        return Task.FromResult((items.Skip((page - 1) * pageSize).Take(pageSize).ToList(), items.Count));
    }

    public Task<List<SchemaChangeEvent>> GetRecentForTableAsync(Guid connectionId, string schemaName, string tableName, int take, CancellationToken ct = default) =>
        Task.FromResult(Events
            .Where(e => e.ConnectionId == connectionId && e.SchemaName == schemaName && e.TableName == tableName)
            .OrderByDescending(e => e.DetectedAt)
            .Take(take)
            .ToList());

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        var removed = Events.RemoveAll(e => e.DetectedAt < cutoff);
        return Task.FromResult(removed);
    }
}
