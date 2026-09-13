using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class FakeSystemSettingsRepository(SystemSettings settings) : ISystemSettingsRepository
{
    public SystemSettings Settings { get; } = settings;

    public static SystemSettings CreateDefault() => new()
    {
        Id = Guid.NewGuid(),
        HealthCheckIntervalSeconds = 30,
        HealthCheckMaxDegreeOfParallelism = 10,
        HealthCheckTimeoutSeconds = 5,
        DiscoveryIntervalSeconds = 60,
        DiscoveryMaxDegreeOfParallelism = 5,
        DiscoveryTimeoutSeconds = 10,
        AlertingIntervalSeconds = 60,
        SlotInactiveMinutes = 5,
        LagWarningSustainedMinutes = 10,
        LagWarningBytes = 50 * 1024 * 1024,
        ConsecutiveHealthCheckFailures = 2,
        HealthyWalStatusesCsv = "reserved,extended",
        ReconciliationIntervalSeconds = 7 * 86400,
        EmailEnabled = false,
        SmtpHost = string.Empty,
        SmtpPort = 587,
        SmtpUseStartTls = true,
        FromAddress = "cdc-monitoring@example.com",
        FromDisplayName = "CDC Monitoring",
        RecipientsCsv = string.Empty,
        UpdatedAt = DateTimeOffset.UtcNow
    };

    public Task<SystemSettings> GetAsync(CancellationToken ct = default) => Task.FromResult(Settings);

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
