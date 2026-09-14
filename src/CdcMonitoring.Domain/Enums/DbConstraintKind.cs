namespace CdcMonitoring.Domain.Enums;

/// <summary>
/// Şema kataloğunda izlenen kısıt türü (pg_constraint.contype karşılığı).
/// </summary>
public enum DbConstraintKind
{
    PrimaryKey,
    ForeignKey,
    Unique,
    Check,
    Exclusion,
    Other
}
