namespace CdcMonitoring.Domain.Entities;

/// <summary>
/// Şema kataloğunda izlenen bir kolon. DbTable gibi silinmez; kaynakta kalmayan
/// kolonlar DroppedAt ile işaretlenir.
/// </summary>
public class DbColumn
{
    public Guid Id { get; set; }
    public Guid TableId { get; set; }
    public DbTable? Table { get; set; }
    public required string ColumnName { get; set; }
    public int OrdinalPosition { get; set; }
    public required string DataType { get; set; }
    public bool IsNullable { get; set; }
    public string? DefaultExpression { get; set; }
    public int? MaxLength { get; set; }
    public int? NumericPrecision { get; set; }
    public int? NumericScale { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string? Comment { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? DroppedAt { get; set; }
}
