using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class SystemSettingsRepository(CdcMonitoringDbContext db) : ISystemSettingsRepository
{
    public Task<SystemSettings> GetAsync(CancellationToken ct = default) =>
        db.SystemSettings.SingleAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
