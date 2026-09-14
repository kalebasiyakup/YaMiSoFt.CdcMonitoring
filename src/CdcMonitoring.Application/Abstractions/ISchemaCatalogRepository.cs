using CdcMonitoring.Application.SchemaCatalog;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

/// <summary>
/// Şema kataloğunun (FR-15) kalıcı deposu. Katalog "güncel durum" olarak tutulur:
/// tarama sonuçları mevcut satırların üzerine yazılır, kaynakta kalmayan nesneler
/// silinmek yerine DroppedAt ile işaretlenir.
/// </summary>
public interface ISchemaCatalogRepository
{
    /// <summary>
    /// Diff için gereken tam görüntü: bir bağlantının kolonları, indeksleri ve kısıtlarıyla
    /// birlikte TÜM tabloları — daha önce düşmüş (DroppedAt dolu) olanlar dahil. Düşmüş
    /// kayıtlar da yüklenir; aksi halde geri eklenen bir tablo için ikinci bir satır
    /// oluşturulup unique index ihlali doğardı.
    /// </summary>
    Task<List<DbTable>> GetTablesForDiffAsync(Guid connectionId, CancellationToken ct = default);

    Task AddTableAsync(DbTable table, CancellationToken ct = default);
    Task AddScanAsync(SchemaScan scan, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    Task<List<SchemaCatalogConnectionStats>> GetConnectionStatsAsync(CancellationToken ct = default);
    Task<(List<DbTable> Items, int TotalCount)> GetPagedTablesAsync(SchemaTableFilter filter, int page, int pageSize, CancellationToken ct = default);
    Task<DbTable?> GetTableWithChildrenAsync(Guid tableId, CancellationToken ct = default);
    /// <summary>connectionId null ise tüm bağlantılardaki şema adları (rapor ekranı filtresi).</summary>
    Task<List<string>> GetSchemaNamesAsync(Guid? connectionId, CancellationToken ct = default);

    /// <summary>
    /// Rapor ekranı için: katalogu "her satır bir kolon" biçiminde düzleştirip sayfalar.
    /// Filtreleme ve sayfalama DB seviyesinde yapılır.
    /// </summary>
    Task<(List<SchemaColumnReportRow> Items, int TotalCount)> GetPagedColumnReportAsync(
        SchemaColumnReportFilter filter, int page, int pageSize, CancellationToken ct = default);
    Task<SchemaScan?> GetLastScanAsync(Guid connectionId, CancellationToken ct = default);

    // Retention temizliği (Ayarlar > Şema Kataloğu) için: cutoff'tan eski tarama kayıtlarını siler.
    Task<int> DeleteScansOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
