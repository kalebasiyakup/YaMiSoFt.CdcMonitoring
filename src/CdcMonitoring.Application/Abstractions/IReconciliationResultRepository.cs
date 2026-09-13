using CdcMonitoring.Application.Reconciliation;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

public interface IReconciliationResultRepository
{
    Task<List<ReconciliationResult>> GetRecentAsync(int take, CancellationToken ct = default);
    Task AddAsync(ReconciliationResult result, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);

    // Retention temizliği (Ayarlar > Veri Tutarlılık Kontrolü) için: cutoff'tan eski tüm kayıtları siler.
    Task<int> DeleteOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default);

    // Veri Tutarlılık Kontrolü ekranındaki filtre + sayfalama için: page 1'den başlar.
    Task<(List<ReconciliationResult> Items, int TotalCount)> GetPagedAsync(
        ReconciliationResultFilter filter, int page, int pageSize, CancellationToken ct = default);
}
