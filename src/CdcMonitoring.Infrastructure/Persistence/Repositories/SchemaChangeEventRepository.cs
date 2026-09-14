using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.SchemaCatalog;
using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class SchemaChangeEventRepository(CdcMonitoringDbContext db) : ISchemaChangeEventRepository
{
    public async Task AddRangeAsync(IEnumerable<SchemaChangeEvent> events, CancellationToken ct = default) =>
        await db.SchemaChangeEvents.AddRangeAsync(events, ct);

    public async Task<(List<SchemaChangeEvent> Items, int TotalCount)> GetPagedAsync(SchemaChangeFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.SchemaChangeEvents.AsNoTracking().AsQueryable();

        if (filter.ConnectionId is { } connectionId)
            query = query.Where(e => e.ConnectionId == connectionId);

        if (filter.ChangeType is { } changeType)
            query = query.Where(e => e.ChangeType == changeType);

        if (filter.Since is { } since)
            query = query.Where(e => e.DetectedAt >= since);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(e => e.DetectedAt)
            .ThenBy(e => e.SchemaName).ThenBy(e => e.TableName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(e => e.Connection)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<List<SchemaChangeEvent>> GetRecentForTableAsync(Guid connectionId, string schemaName, string tableName, int take, CancellationToken ct = default) =>
        db.SchemaChangeEvents
            .AsNoTracking()
            .Where(e => e.ConnectionId == connectionId && e.SchemaName == schemaName && e.TableName == tableName)
            .OrderByDescending(e => e.DetectedAt)
            .Take(take)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default) =>
        db.SchemaChangeEvents.Where(e => e.DetectedAt < cutoff).ExecuteDeleteAsync(ct);
}
