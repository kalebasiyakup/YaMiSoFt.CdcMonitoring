using CdcMonitoring.Application.SchemaCatalog;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

/// <summary>
/// İki katalog taraması arasında tespit edilen şema değişikliklerinin geçmişi.
/// Katalog tabloları yalnızca güncel durumu tuttuğu için "ne zaman ne değişti"
/// sorusunun tek kaynağı burasıdır.
/// </summary>
public interface ISchemaChangeEventRepository
{
    Task AddRangeAsync(IEnumerable<SchemaChangeEvent> events, CancellationToken ct = default);
    Task<(List<SchemaChangeEvent> Items, int TotalCount)> GetPagedAsync(SchemaChangeFilter filter, int page, int pageSize, CancellationToken ct = default);
    Task<List<SchemaChangeEvent>> GetRecentForTableAsync(Guid connectionId, string schemaName, string tableName, int take, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    // Retention temizliği (Ayarlar > Şema Kataloğu) için: cutoff'tan eski tüm kayıtları siler.
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);
}
