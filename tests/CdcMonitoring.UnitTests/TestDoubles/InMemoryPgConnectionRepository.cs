using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class InMemoryPgConnectionRepository : IPgConnectionRepository
{
    private readonly List<PgConnection> _items = [];

    public Task<PgConnection?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(_items.FirstOrDefault(c => c.Id == id));

    public Task<List<PgConnection>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(_items.OrderBy(c => c.Name).ToList());

    public Task<List<PgConnection>> GetActiveAsync(CancellationToken ct = default) =>
        Task.FromResult(_items.Where(c => c.IsActive).ToList());

    public Task<bool> ExistsAsync(string host, int port, string databaseName, Guid? excludeId = null, CancellationToken ct = default) =>
        Task.FromResult(_items.Any(c =>
            c.Host == host && c.Port == port && c.DatabaseName == databaseName &&
            (excludeId == null || c.Id != excludeId)));

    public Task AddAsync(PgConnection connection, CancellationToken ct = default)
    {
        _items.Add(connection);
        return Task.CompletedTask;
    }

    public void Remove(PgConnection connection) => _items.RemoveAll(c => c.Id == connection.Id);

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
