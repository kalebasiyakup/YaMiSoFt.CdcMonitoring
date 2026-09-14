using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.SchemaCatalog;
using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class SchemaCatalogRepository(CdcMonitoringDbContext db) : ISchemaCatalogRepository
{
    // AsSplitQuery: üç koleksiyon tek sorguda Include edilirse EF kartezyen çarpım üretir
    // (tablo başına kolon × indeks × kısıt satırı) — 20 kolon/5 indeks/6 kısıtlı bir tablo
    // için 600 satır. Binlerce tablolu bir katalogda bu hem ağı hem belleği gereksiz yere
    // şişirir; ayrı sorgular (koleksiyon başına bir round-trip) belirgin biçimde ucuzdur.
    public Task<List<DbTable>> GetTablesForDiffAsync(Guid connectionId, CancellationToken ct = default) =>
        db.DbTables
            .Where(t => t.ConnectionId == connectionId)
            .Include(t => t.Columns)
            .Include(t => t.Indexes)
            .Include(t => t.Constraints)
            .AsSplitQuery()
            .ToListAsync(ct);

    public async Task AddTableAsync(DbTable table, CancellationToken ct = default) =>
        await db.DbTables.AddAsync(table, ct);

    public async Task AddScanAsync(SchemaScan scan, CancellationToken ct = default) =>
        await db.SchemaScans.AddAsync(scan, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    public async Task<List<SchemaCatalogConnectionStats>> GetConnectionStatsAsync(CancellationToken ct = default)
    {
        var tableStats = await db.DbTables
            .Where(t => t.DroppedAt == null)
            .GroupBy(t => t.ConnectionId)
            .Select(g => new
            {
                ConnectionId = g.Key,
                TableCount = g.Count(),
                TotalSizeBytes = g.Sum(t => t.TotalSizeBytes) ?? 0
            })
            .ToDictionaryAsync(x => x.ConnectionId, ct);

        var columnCounts = await db.DbColumns
            .Where(c => c.DroppedAt == null && c.Table!.DroppedAt == null)
            .GroupBy(c => c.Table!.ConnectionId)
            .Select(g => new { ConnectionId = g.Key, ColumnCount = g.Count() })
            .ToDictionaryAsync(x => x.ConnectionId, x => x.ColumnCount, ct);

        var lastScans = await db.SchemaScans
            .GroupBy(s => s.ConnectionId)
            .Select(g => g.OrderByDescending(s => s.StartedAt).First())
            .ToDictionaryAsync(s => s.ConnectionId, ct);

        var connections = await db.PgConnections
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return [.. connections.Select(c =>
        {
            var tables = tableStats.GetValueOrDefault(c.Id);
            var lastScan = lastScans.GetValueOrDefault(c.Id);

            return new SchemaCatalogConnectionStats(
                c.Id,
                c.Name,
                c.Host,
                c.Port,
                c.DatabaseName,
                c.EnvironmentTag,
                c.IsActive,
                tables?.TableCount ?? 0,
                columnCounts.GetValueOrDefault(c.Id),
                tables?.TotalSizeBytes ?? 0,
                lastScan?.StartedAt,
                lastScan?.Success,
                lastScan?.ErrorMessage);
        })];
    }

    public async Task<(List<DbTable> Items, int TotalCount)> GetPagedTablesAsync(SchemaTableFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        var query = db.DbTables.AsNoTracking().Where(t => t.ConnectionId == filter.ConnectionId);

        if (!filter.IncludeDropped)
            query = query.Where(t => t.DroppedAt == null);

        if (!string.IsNullOrWhiteSpace(filter.SchemaName))
            query = query.Where(t => t.SchemaName == filter.SchemaName);

        if (filter.OnlyPublished)
            query = query.Where(t => t.IsPublished);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            // Katalogda arama hem tablo adında hem de kolon adlarında yapılır: "şu kolon
            // hangi tablolarda var" katalogun en sık sorulan sorusu.
            var term = $"%{filter.SearchText.Trim()}%";
            query = query.Where(t =>
                EF.Functions.ILike(t.SchemaName + "." + t.TableName, term) ||
                t.Columns.Any(c => EF.Functions.ILike(c.ColumnName, term)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(t => t.SchemaName).ThenBy(t => t.TableName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(t => t.Columns.OrderBy(c => c.OrdinalPosition))
            .Include(t => t.Indexes.OrderBy(i => i.IndexName))
            .Include(t => t.Constraints.OrderBy(c => c.ConstraintName))
            .AsSplitQuery()
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<DbTable?> GetTableWithChildrenAsync(Guid tableId, CancellationToken ct = default) =>
        db.DbTables
            .AsNoTracking()
            .Include(t => t.Columns.OrderBy(c => c.OrdinalPosition))
            .Include(t => t.Indexes.OrderBy(i => i.IndexName))
            .Include(t => t.Constraints.OrderBy(c => c.ConstraintName))
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == tableId, ct);

    public Task<List<string>> GetSchemaNamesAsync(Guid? connectionId, CancellationToken ct = default) =>
        db.DbTables
            .Where(t => t.DroppedAt == null)
            .Where(t => connectionId == null || t.ConnectionId == connectionId)
            .Select(t => t.SchemaName)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(ct);

    public async Task<(List<SchemaColumnReportRow> Items, int TotalCount)> GetPagedColumnReportAsync(
        SchemaColumnReportFilter filter, int page, int pageSize, CancellationToken ct = default)
    {
        // Filtreleme ve sıralama entity üzerinde yapılır, projeksiyon EN SONDA eklenir:
        // EF, kendi oluşturduğu bir kayıt (record) projeksiyonunun üzerine konan Where/OrderBy
        // ifadelerini SQL'e çeviremez ve sorgu istemci tarafına düşmek yerine hata verir.
        var query = db.DbColumns.AsNoTracking().AsQueryable();

        if (filter.ConnectionId is { } connectionId)
            query = query.Where(c => c.Table!.ConnectionId == connectionId);

        if (!filter.IncludeDropped)
            query = query.Where(c => c.DroppedAt == null && c.Table!.DroppedAt == null);

        if (!string.IsNullOrWhiteSpace(filter.SchemaName))
            query = query.Where(c => c.Table!.SchemaName == filter.SchemaName);

        if (filter.OnlyPublished)
            query = query.Where(c => c.Table!.IsPublished);

        // "Açıklaması olmayanlar": PostgreSQL'de COMMENT girilmemiş kolonları listeleyerek
        // dokümantasyon boşluklarını çıkarmaya yarar.
        if (filter.OnlyUndocumented)
            query = query.Where(c => c.Comment == null || c.Comment == "");

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var term = $"%{filter.SearchText.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Table!.SchemaName + "." + c.Table.TableName, term) ||
                EF.Functions.ILike(c.ColumnName, term) ||
                (c.Comment != null && EF.Functions.ILike(c.Comment, term)) ||
                (c.Table!.Comment != null && EF.Functions.ILike(c.Table.Comment, term)));
        }

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Table!.Connection!.Name)
            .ThenBy(c => c.Table!.SchemaName)
            .ThenBy(c => c.Table!.TableName)
            .ThenBy(c => c.OrdinalPosition)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new SchemaColumnReportRow(
                c.Table!.ConnectionId,
                c.Table.Connection!.Name,
                c.Table.Connection.DatabaseName,
                c.Table.SchemaName,
                c.Table.TableName,
                c.Table.Kind,
                c.Table.Comment,
                c.Table.IsPublished,
                c.ColumnName,
                c.OrdinalPosition,
                c.DataType,
                c.MaxLength,
                c.NumericPrecision,
                c.NumericScale,
                c.IsNullable,
                c.IsPrimaryKey,
                c.DefaultExpression,
                c.Comment,
                c.Table.DroppedAt,
                c.DroppedAt))
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<SchemaScan?> GetLastScanAsync(Guid connectionId, CancellationToken ct = default) =>
        db.SchemaScans
            .AsNoTracking()
            .Where(s => s.ConnectionId == connectionId)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);

    public Task<int> DeleteScansOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default) =>
        db.SchemaScans.Where(s => s.StartedAt < cutoff).ExecuteDeleteAsync(ct);
}
