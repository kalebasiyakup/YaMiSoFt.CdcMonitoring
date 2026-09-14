namespace CdcMonitoring.Domain.Enums;

/// <summary>
/// İki katalog taraması arasında tespit edilen şema değişikliğinin türü.
/// </summary>
public enum SchemaChangeType
{
    TableAdded,
    TableDropped,
    ColumnAdded,
    ColumnDropped,
    ColumnTypeChanged,
    ColumnNullabilityChanged,
    ColumnDefaultChanged,
    IndexAdded,
    IndexDropped,
    IndexChanged,
    ConstraintAdded,
    ConstraintDropped,
    ConstraintChanged
}
