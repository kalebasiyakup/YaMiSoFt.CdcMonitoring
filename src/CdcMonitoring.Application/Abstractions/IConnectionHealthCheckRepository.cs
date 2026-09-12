using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

public interface IConnectionHealthCheckRepository
{
    Task AddAsync(ConnectionHealthCheck result, CancellationToken ct = default);
    Task<ConnectionHealthCheck?> GetLatestAsync(Guid connectionId, CancellationToken ct = default);
    Task<List<ConnectionHealthCheck>> GetRecentAsync(Guid connectionId, int take, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
