namespace CdcMonitoring.Domain.Entities;

/// <summary>
/// Şema kataloğunda izlenen bir indeks. Definition, pg_get_indexdef çıktısının
/// aynen saklanmış halidir — uygulama bu metni hiçbir zaman çalıştırmaz (FR-14).
/// </summary>
public class DbIndex
{
    public Guid Id { get; set; }
    public Guid TableId { get; set; }
    public DbTable? Table { get; set; }
    public required string IndexName { get; set; }
    public bool IsUnique { get; set; }
    public bool IsPrimary { get; set; }
    public required string ColumnsCsv { get; set; }
    public required string Definition { get; set; }
    public long? SizeBytes { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? DroppedAt { get; set; }
}
