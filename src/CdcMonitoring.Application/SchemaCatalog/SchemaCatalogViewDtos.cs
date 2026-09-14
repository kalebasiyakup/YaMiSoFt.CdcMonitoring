using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.SchemaCatalog;

/// <summary>Katalog ana ekranındaki bağlantı (veritabanı) kartı için özet.</summary>
public record SchemaCatalogConnectionStats(
    Guid ConnectionId,
    string ConnectionName,
    string Host,
    int Port,
    string DatabaseName,
    string EnvironmentTag,
    bool IsActive,
    int TableCount,
    int ColumnCount,
    long TotalSizeBytes,
    DateTimeOffset? LastScanAt,
    bool? LastScanSuccess,
    string? LastScanError);

/// <summary>
/// Tablo listesi filtresi. SearchText hem şema.tablo adında hem de kolon adlarında aranır —
/// "hangi tablolarda customer_id var" sorusu katalogun en sık kullanılan sorgusudur.
/// </summary>
public record SchemaTableFilter(
    Guid ConnectionId,
    string? SchemaName = null,
    string? SearchText = null,
    bool IncludeDropped = false,
    bool OnlyPublished = false);

public record SchemaChangeFilter(
    Guid? ConnectionId = null,
    SchemaChangeType? ChangeType = null,
    DateTimeOffset? Since = null);

/// <summary>
/// Rapor ekranının satırı: katalogun tablo/kolon hiyerarşisi tek düz listeye açılmış hali
/// (her satır bir kolon), tablo ve kolon açıklamalarıyla birlikte.
/// </summary>
public record SchemaColumnReportRow(
    Guid ConnectionId,
    string ConnectionName,
    string DatabaseName,
    string SchemaName,
    string TableName,
    DbObjectKind Kind,
    string? TableComment,
    bool IsPublished,
    string ColumnName,
    int OrdinalPosition,
    string DataType,
    int? MaxLength,
    int? NumericPrecision,
    int? NumericScale,
    bool IsNullable,
    bool IsPrimaryKey,
    string? DefaultExpression,
    string? ColumnComment,
    DateTimeOffset? TableDroppedAt,
    DateTimeOffset? ColumnDroppedAt)
{
    public bool IsDropped => TableDroppedAt is not null || ColumnDroppedAt is not null;
}

/// <summary>
/// Rapor filtresi. SearchText tablo adı, kolon adı VE açıklamalarda birden arar;
/// OnlyUndocumented, açıklaması girilmemiş kolonları listeleyerek dokümantasyon
/// boşluklarını çıkarmaya yarar.
/// </summary>
public record SchemaColumnReportFilter(
    Guid? ConnectionId = null,
    string? SchemaName = null,
    string? SearchText = null,
    bool IncludeDropped = false,
    bool OnlyPublished = false,
    bool OnlyUndocumented = false);
