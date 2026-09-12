using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

public interface ISystemSettingsRepository
{
    Task<SystemSettings> GetAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
