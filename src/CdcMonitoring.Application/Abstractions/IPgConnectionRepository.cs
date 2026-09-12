using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

public interface IPgConnectionRepository
{
    Task<PgConnection?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PgConnection>> GetAllAsync(CancellationToken ct = default);
    Task<List<PgConnection>> GetActiveAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string host, int port, string databaseName, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(PgConnection connection, CancellationToken ct = default);
    void Remove(PgConnection connection);
    Task SaveChangesAsync(CancellationToken ct = default);
}
