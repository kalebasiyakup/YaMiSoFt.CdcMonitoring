using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.SchemaCatalog;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class InMemorySchemaCatalogRepository : ISchemaCatalogRepository
{
    public List<DbTable> Tables { get; } = [];
    public List<SchemaScan> Scans { get; } = [];

    public Task<List<DbTable>> GetTablesForDiffAsync(Guid connectionId, CancellationToken ct = default) =>
        Task.FromResult(Tables.Where(t => t.ConnectionId == connectionId).ToList());

    public Task AddTableAsync(DbTable table, CancellationToken ct = default)
    {
        Tables.Add(table);
        return Task.CompletedTask;
    }

    public Task AddScanAsync(SchemaScan scan, CancellationToken ct = default)
    {
        Scans.Add(scan);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<List<SchemaCatalogConnectionStats>> GetConnectionStatsAsync(CancellationToken ct = default) =>
        Task.FromResult(new List<SchemaCatalogConnectionStats>());

    public Task<(List<DbTable> Items, int TotalCount)> GetPagedTablesAsync(SchemaTableFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var items = Tables.Where(t => t.ConnectionId == filter.ConnectionId).ToList();
        return Task.FromResult((items, items.Count));
    }

    public Task<DbTable?> GetTableWithChildrenAsync(Guid tableId, CancellationToken ct = default) =>
        Task.FromResult(Tables.FirstOrDefault(t => t.Id == tableId));

    public Task<List<string>> GetSchemaNamesAsync(Guid? connectionId, CancellationToken ct = default) =>
        Task.FromResult(Tables
            .Where(t => connectionId == null || t.ConnectionId == connectionId)
            .Select(t => t.SchemaName).Distinct().Order().ToList());

    public Task<(List<SchemaColumnReportRow> Items, int TotalCount)> GetPagedColumnReportAsync(
        SchemaColumnReportFilter filter, int page, int pageSize, CancellationToken ct = default) =>
        Task.FromResult((new List<SchemaColumnReportRow>(), 0));

    public Task<SchemaScan?> GetLastScanAsync(Guid connectionId, CancellationToken ct = default) =>
        Task.FromResult(Scans.Where(s => s.ConnectionId == connectionId).OrderByDescending(s => s.StartedAt).FirstOrDefault());

    public Task<int> DeleteScansOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default)
    {
        var removed = Scans.RemoveAll(s => s.StartedAt < cutoff);
        return Task.FromResult(removed);
    }
}
