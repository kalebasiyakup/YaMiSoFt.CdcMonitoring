using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

public interface IReconciliationResultRepository
{
    Task<List<ReconciliationResult>> GetRecentAsync(int take, CancellationToken ct = default);
    Task AddAsync(ReconciliationResult result, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
