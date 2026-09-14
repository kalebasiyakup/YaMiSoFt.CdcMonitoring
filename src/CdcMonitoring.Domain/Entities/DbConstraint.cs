using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Domain.Entities;

/// <summary>
/// Şema kataloğunda izlenen bir kısıt (PK/FK/unique/check). Definition,
/// pg_get_constraintdef çıktısının aynen saklanmış halidir — uygulama bu metni
/// hiçbir zaman çalıştırmaz (FR-14).
/// </summary>
public class DbConstraint
{
    public Guid Id { get; set; }
    public Guid TableId { get; set; }
    public DbTable? Table { get; set; }
    public required string ConstraintName { get; set; }
    public DbConstraintKind Kind { get; set; } = DbConstraintKind.Other;
    public required string ColumnsCsv { get; set; }

    /// <summary>Yalnızca ForeignKey kısıtlarında dolu: hedef tablonun şeması.</summary>
    public string? ReferencedSchema { get; set; }
    public string? ReferencedTable { get; set; }
    public string? ReferencedColumnsCsv { get; set; }

    public required string Definition { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? DroppedAt { get; set; }
}
