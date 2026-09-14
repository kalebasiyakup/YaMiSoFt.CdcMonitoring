using ClosedXML.Excel;
using CdcMonitoring.Application.SchemaCatalog;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Web.Services;

/// <summary>
/// Şema Raporu ekranındaki listeyi .xlsx dosyasına dönüştürür. CSV yerine gerçek bir Excel
/// dosyası üretilir: CSV'de ayraç ve kodlama Excel'in bölge ayarına göre değiştiği için
/// (Türkçe Excel ";" bekler) Türkçe karakterli açıklamalar sık sık bozuk açılıyordu.
/// </summary>
public static class SchemaCatalogExcelExporter
{
    /// <summary>
    /// Export edilecek azami satır sayısı. Rapor tüm katalogu kapsayabildiği için sınırsız
    /// bırakmak, çok tablolu ortamlarda tek istekte yüz binlerce satırı belleğe almak demektir.
    /// </summary>
    public const int MaxRows = 50_000;

    public static byte[] Build(IReadOnlyList<SchemaColumnReportRow> rows, DateTimeOffset generatedAt)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Şema Raporu");

        string[] headers =
        [
            "Veritabanı", "Sunucu DB", "Şema", "Tablo", "Nesne Türü", "Tablo Açıklaması",
            "CDC (Publication)", "Sıra", "Kolon", "Tip", "Null", "PK", "Varsayılan",
            "Kolon Açıklaması", "Durum"
        ];

        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var headerRow = sheet.Row(1);
        headerRow.Style.Font.Bold = true;
        headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9ECEF");

        var r = 2;
        foreach (var row in rows)
        {
            sheet.Cell(r, 1).Value = row.ConnectionName;
            sheet.Cell(r, 2).Value = row.DatabaseName;
            sheet.Cell(r, 3).Value = row.SchemaName;
            sheet.Cell(r, 4).Value = row.TableName;
            sheet.Cell(r, 5).Value = KindLabel(row.Kind);
            sheet.Cell(r, 6).Value = row.TableComment ?? string.Empty;
            sheet.Cell(r, 7).Value = row.IsPublished ? "Evet" : "Hayır";
            sheet.Cell(r, 8).Value = row.OrdinalPosition;
            sheet.Cell(r, 9).Value = row.ColumnName;
            sheet.Cell(r, 10).Value = FormatType(row);
            sheet.Cell(r, 11).Value = row.IsNullable ? "NULL" : "NOT NULL";
            sheet.Cell(r, 12).Value = row.IsPrimaryKey ? "Evet" : string.Empty;
            sheet.Cell(r, 13).Value = row.DefaultExpression ?? string.Empty;
            sheet.Cell(r, 14).Value = row.ColumnComment ?? string.Empty;
            sheet.Cell(r, 15).Value = row.IsDropped ? "Silinmiş" : "Mevcut";
            r++;
        }

        if (rows.Count > 0)
        {
            // Excel'de başlık satırı sabit kalsın ve sütunlar filtrelenebilsin.
            sheet.SheetView.FreezeRows(1);
            sheet.Range(1, 1, rows.Count + 1, headers.Length).SetAutoFilter();
        }

        sheet.Columns().AdjustToContents(1, 200, 8, 60);

        var info = workbook.AddWorksheet("Bilgi");
        info.Cell(1, 1).Value = "Oluşturulma zamanı (UTC)";
        info.Cell(1, 2).Value = generatedAt.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        info.Cell(2, 1).Value = "Satır sayısı";
        info.Cell(2, 2).Value = rows.Count;
        info.Cell(3, 1).Value = "Kaynak";
        info.Cell(3, 2).Value = "CDC Monitoring — Şema Kataloğu (salt-okuma ile toplanan şema bilgisi)";
        if (rows.Count >= MaxRows)
        {
            info.Cell(4, 1).Value = "Uyarı";
            info.Cell(4, 2).Value = $"Sonuç {MaxRows:N0} satır sınırına ulaştı; liste kırpılmış olabilir. Filtreleri daraltın.";
        }
        info.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string KindLabel(DbObjectKind kind) => kind switch
    {
        DbObjectKind.PartitionedTable => "Bölümlenmiş Tablo",
        DbObjectKind.View => "Görünüm",
        DbObjectKind.MaterializedView => "Materyalize Görünüm",
        DbObjectKind.ForeignTable => "Dış Tablo",
        _ => "Tablo"
    };

    private static string FormatType(SchemaColumnReportRow row)
    {
        if (row.MaxLength is { } length)
            return $"{row.DataType}({length})";

        if (row.DataType is "numeric" or "decimal" && row.NumericPrecision is { } precision)
            return row.NumericScale is { } scale && scale > 0
                ? $"{row.DataType}({precision},{scale})"
                : $"{row.DataType}({precision})";

        return row.DataType;
    }
}
