using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Web.Components.Pages.SchemaCatalog;

/// <summary>Şema değişiklik türlerinin ekranda gösterilen Türkçe karşılıkları.</summary>
public static class SchemaChangeLabels
{
    public static string Describe(SchemaChangeType changeType) => changeType switch
    {
        SchemaChangeType.TableAdded => "Tablo eklendi",
        SchemaChangeType.TableDropped => "Tablo silindi",
        SchemaChangeType.ColumnAdded => "Kolon eklendi",
        SchemaChangeType.ColumnDropped => "Kolon silindi",
        SchemaChangeType.ColumnTypeChanged => "Kolon tipi değişti",
        SchemaChangeType.ColumnNullabilityChanged => "Kolon NULL kabulü değişti",
        SchemaChangeType.ColumnDefaultChanged => "Kolon varsayılanı değişti",
        SchemaChangeType.IndexAdded => "İndeks eklendi",
        SchemaChangeType.IndexDropped => "İndeks silindi",
        SchemaChangeType.IndexChanged => "İndeks değişti",
        SchemaChangeType.ConstraintAdded => "Kısıt eklendi",
        SchemaChangeType.ConstraintDropped => "Kısıt silindi",
        SchemaChangeType.ConstraintChanged => "Kısıt değişti",
        _ => changeType.ToString()
    };

    public static string BadgeClass(SchemaChangeType changeType) => changeType switch
    {
        SchemaChangeType.TableDropped or SchemaChangeType.ColumnDropped
            or SchemaChangeType.IndexDropped or SchemaChangeType.ConstraintDropped => "bg-danger",
        SchemaChangeType.TableAdded or SchemaChangeType.ColumnAdded
            or SchemaChangeType.IndexAdded or SchemaChangeType.ConstraintAdded => "bg-success",
        _ => "bg-warning text-dark"
    };
}
