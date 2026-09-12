using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class PgConnectionRepository(CdcMonitoringDbContext db) : IPgConnectionRepository
{
    public Task<PgConnection?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.PgConnections.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<List<PgConnection>> GetAllAsync(CancellationToken ct = default) =>
        db.PgConnections.OrderBy(c => c.Name).ToListAsync(ct);

    public Task<List<PgConnection>> GetActiveAsync(CancellationToken ct = default) =>
        db.PgConnections.Where(c => c.IsActive).ToListAsync(ct);

    public Task<bool> ExistsAsync(string host, int port, string databaseName, Guid? excludeId = null, CancellationToken ct = default) =>
        db.PgConnections.AnyAsync(c =>
            c.Host == host && c.Port == port && c.DatabaseName == databaseName &&
            (excludeId == null || c.Id != excludeId), ct);

    public async Task AddAsync(PgConnection connection, CancellationToken ct = default) =>
        await db.PgConnections.AddAsync(connection, ct);

    public void Remove(PgConnection connection) => db.PgConnections.Remove(connection);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
