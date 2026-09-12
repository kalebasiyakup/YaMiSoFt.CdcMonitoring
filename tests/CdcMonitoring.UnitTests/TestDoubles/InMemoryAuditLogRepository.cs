using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class InMemoryAuditLogRepository : IAuditLogRepository
{
    public List<AuditLog> Entries { get; } = [];

    public Task AddAsync(AuditLog entry, CancellationToken ct = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}
