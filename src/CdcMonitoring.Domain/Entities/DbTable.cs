using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Domain.Entities;

/// <summary>
/// Şema kataloğunda izlenen bir tablo/görünüm. Her tarama bu satırı günceller; kayıt
/// silinmez, kaynakta artık bulunamayan nesneler DroppedAt ile işaretlenir (böylece
/// "ne zaman kayboldu" bilgisi korunur).
/// </summary>
public class DbTable
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public PgConnection? Connection { get; set; }
    public required string SchemaName { get; set; }
    public required string TableName { get; set; }
    public DbObjectKind Kind { get; set; } = DbObjectKind.Table;

    /// <summary>pg_class.reltuples — planlayıcının tahmini satır sayısı (COUNT(*) değildir).</summary>
    public long? EstimatedRowCount { get; set; }

    /// <summary>pg_total_relation_size — indeks ve TOAST dahil toplam boyut (byte).</summary>
    public long? TotalSizeBytes { get; set; }

    public bool HasPrimaryKey { get; set; }

    /// <summary>Tablonun bu bağlantıdaki herhangi bir publication'a dahil olup olmadığı.</summary>
    public bool IsPublished { get; set; }

    public string? Comment { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? DroppedAt { get; set; }

    public List<DbColumn> Columns { get; set; } = [];
    public List<DbIndex> Indexes { get; set; } = [];
    public List<DbConstraint> Constraints { get; set; } = [];
}
