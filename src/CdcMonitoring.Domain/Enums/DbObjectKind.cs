namespace CdcMonitoring.Domain.Enums;

/// <summary>
/// Şema kataloğunda izlenen ilişkisel nesne türü (pg_class.relkind karşılığı).
/// </summary>
public enum DbObjectKind
{
    Table,
    PartitionedTable,
    View,
    MaterializedView,
    ForeignTable
}
