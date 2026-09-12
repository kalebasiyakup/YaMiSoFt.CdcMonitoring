using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.Abstractions;

public interface IAlertEventRepository
{
    Task<AlertEvent?> GetActiveAsync(AlertType type, Guid? connectionId, Guid? relationshipId, CancellationToken ct = default);
    Task<List<AlertEvent>> GetRecentAsync(int take, CancellationToken ct = default);
    Task AddAsync(AlertEvent alertEvent, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
