using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.SchemaCatalog;

/// <summary>
/// Bir bağlantının tek bir taramada okunan şema kataloğu. Kalıcı hale getirme
/// (DbTable/DbColumn/DbIndex/DbConstraint) SchemaCatalogService'in işidir; bu tipler
/// yalnızca PostgreSQL'den okunan ham görüntüyü taşır.
/// </summary>
public record SchemaCatalogSnapshot(List<CatalogTableInfo> Tables)
{
    public int ColumnCount => Tables.Sum(t => t.Columns.Count);
}

public class CatalogTableInfo
{
    public required string SchemaName { get; init; }
    public required string TableName { get; init; }
    public DbObjectKind Kind { get; init; } = DbObjectKind.Table;

    /// <summary>pg_class.reltuples; hiç ANALYZE edilmemiş tablolarda null bırakılır.</summary>
    public long? EstimatedRowCount { get; init; }

    public long? TotalSizeBytes { get; init; }
    public string? Comment { get; init; }

    /// <summary>Tablo bu bağlantıdaki herhangi bir publication'a dahil mi.</summary>
    public bool IsPublished { get; set; }

    public List<CatalogColumnInfo> Columns { get; } = [];
    public List<CatalogIndexInfo> Indexes { get; } = [];
    public List<CatalogConstraintInfo> Constraints { get; } = [];

    public bool HasPrimaryKey => Constraints.Any(c => c.Kind == DbConstraintKind.PrimaryKey);
}

public record CatalogColumnInfo(
    string Name,
    int OrdinalPosition,
    string DataType,
    bool IsNullable,
    string? DefaultExpression,
    int? MaxLength,
    int? NumericPrecision,
    int? NumericScale,
    string? Comment)
{
    /// <summary>Kolonun birincil anahtara dahil olup olmadığı; PK kısıtından türetilir.</summary>
    public bool IsPrimaryKey { get; set; }
}

public record CatalogIndexInfo(
    string Name,
    bool IsUnique,
    bool IsPrimary,
    string ColumnsCsv,
    string Definition,
    long? SizeBytes);

public record CatalogConstraintInfo(
    string Name,
    DbConstraintKind Kind,
    string ColumnsCsv,
    string? ReferencedSchema,
    string? ReferencedTable,
    string? ReferencedColumnsCsv,
    string Definition);
