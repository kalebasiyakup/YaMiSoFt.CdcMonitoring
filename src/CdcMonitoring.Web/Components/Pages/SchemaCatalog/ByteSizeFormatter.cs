namespace CdcMonitoring.Web.Components.Pages.SchemaCatalog;

/// <summary>
/// Katalog ekranlarında tablo/indeks boyutlarını okunabilir biçimde gösterir
/// (pg_total_relation_size byte döner, ham sayı ekranda kullanışsızdır).
/// </summary>
public static class ByteSizeFormatter
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB", "PB"];

    public static string Format(long? bytes, string fallback = "-")
    {
        if (bytes is null)
            return fallback;

        var value = (double)bytes.Value;
        var unit = 0;

        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0 ? $"{bytes} {Units[unit]}" : $"{value:0.#} {Units[unit]}";
    }
}
