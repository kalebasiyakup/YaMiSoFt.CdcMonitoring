using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class AuditLogRepository(CdcMonitoringDbContext db) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog entry, CancellationToken ct = default)
    {
        await db.AuditLogs.AddAsync(entry, ct);
        await db.SaveChangesAsync(ct);
    }
}
