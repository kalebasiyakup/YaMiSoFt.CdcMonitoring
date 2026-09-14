using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Common;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.SchemaCatalog;

/// <summary>
/// Şema Kataloğu ekranının okuma tarafı: bağlantı özetleri, sayfalı tablo listesi,
/// tablo detayı ve değişiklik günlüğü.
/// </summary>
public class SchemaCatalogQueryService(
    ISchemaCatalogRepository catalog,
    ISchemaChangeEventRepository changeEvents)
{
    public Task<List<SchemaCatalogConnectionStats>> GetConnectionStatsAsync(CancellationToken ct = default) =>
        catalog.GetConnectionStatsAsync(ct);

    public async Task<PagedResult<DbTable>> GetTablesAsync(SchemaTableFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await catalog.GetPagedTablesAsync(filter, page, pageSize, ct);
        return new PagedResult<DbTable>(items, totalCount, page, pageSize);
    }

    public Task<DbTable?> GetTableAsync(Guid tableId, CancellationToken ct = default) =>
        catalog.GetTableWithChildrenAsync(tableId, ct);

    public Task<List<string>> GetSchemaNamesAsync(Guid? connectionId, CancellationToken ct = default) =>
        catalog.GetSchemaNamesAsync(connectionId, ct);

    public async Task<PagedResult<SchemaColumnReportRow>> GetColumnReportAsync(
        SchemaColumnReportFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await catalog.GetPagedColumnReportAsync(filter, page, pageSize, ct);
        return new PagedResult<SchemaColumnReportRow>(items, totalCount, page, pageSize);
    }

    public Task<SchemaScan?> GetLastScanAsync(Guid connectionId, CancellationToken ct = default) =>
        catalog.GetLastScanAsync(connectionId, ct);

    public async Task<PagedResult<SchemaChangeEvent>> GetChangesAsync(SchemaChangeFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var (items, totalCount) = await changeEvents.GetPagedAsync(filter, page, pageSize, ct);
        return new PagedResult<SchemaChangeEvent>(items, totalCount, page, pageSize);
    }

    public Task<List<SchemaChangeEvent>> GetTableChangesAsync(Guid connectionId, string schemaName, string tableName, int take = 20, CancellationToken ct = default) =>
        changeEvents.GetRecentForTableAsync(connectionId, schemaName, tableName, take, ct);
}
