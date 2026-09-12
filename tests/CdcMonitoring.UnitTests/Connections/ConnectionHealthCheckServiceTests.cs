using CdcMonitoring.Application.Common;
using CdcMonitoring.Application.HealthChecks;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CdcMonitoring.UnitTests.Connections;

public class ConnectionHealthCheckServiceTests
{
    private static PgConnection NewConnection(string name) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Host = "db.internal",
        Port = 5432,
        DatabaseName = "orders",
        Username = "monitor_ro",
        EncryptedPassword = "enc:s3cr3t!",
        EnvironmentTag = "Prod-DC1",
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        CreatedBy = "system"
    };

    [Fact]
    public async Task RunOnceAsync_checks_every_active_connection_and_persists_results()
    {
        var connections = new InMemoryPgConnectionRepository();
        var c1 = NewConnection("db-1");
        var c2 = NewConnection("db-2");
        await connections.AddAsync(c1);
        await connections.AddAsync(c2);

        var healthRepo = new InMemoryConnectionHealthCheckRepository();
        var metrics = new RecordingMetricsRecorder();
        var checker = new FakeConnectivityChecker(c => new ConnectivityCheckResult(true, 12.3, "PostgreSQL 16.2", null));

        var settings = FakeSystemSettingsRepository.CreateDefault();
        settings.HealthCheckMaxDegreeOfParallelism = 2;
        settings.HealthCheckTimeoutSeconds = 5;

        var service = new ConnectionHealthCheckService(
            connections, healthRepo, new FakePasswordProtector(), checker, metrics,
            new FixedClock(DateTimeOffset.UtcNow),
            new FakeSystemSettingsRepository(settings),
            NullLogger<ConnectionHealthCheckService>.Instance);

        await service.RunOnceAsync();

        Assert.Equal(2, healthRepo.Results.Count);
        Assert.All(healthRepo.Results, r => Assert.True(r.IsUp));
        Assert.Equal(2, metrics.HealthRecords.Count);
        Assert.Single(metrics.ScanCycleRecords);
    }

    [Fact]
    public async Task RunOnceAsync_records_failure_without_throwing_when_connectivity_check_fails()
    {
        var connections = new InMemoryPgConnectionRepository();
        var c1 = NewConnection("db-down");
        await connections.AddAsync(c1);

        var healthRepo = new InMemoryConnectionHealthCheckRepository();
        var metrics = new RecordingMetricsRecorder();
        var checker = new FakeConnectivityChecker(_ => new ConnectivityCheckResult(false, null, null, "connection refused"));

        var service = new ConnectionHealthCheckService(
            connections, healthRepo, new FakePasswordProtector(), checker, metrics,
            new FixedClock(DateTimeOffset.UtcNow),
            new FakeSystemSettingsRepository(FakeSystemSettingsRepository.CreateDefault()),
            NullLogger<ConnectionHealthCheckService>.Instance);

        await service.RunOnceAsync();

        var result = Assert.Single(healthRepo.Results);
        Assert.False(result.IsUp);
        Assert.Equal("connection refused", result.ErrorMessage);
    }

    [Fact]
    public async Task RunOnceAsync_skips_inactive_connections()
    {
        var connections = new InMemoryPgConnectionRepository();
        var inactive = NewConnection("db-inactive");
        inactive.IsActive = false;
        await connections.AddAsync(inactive);

        var healthRepo = new InMemoryConnectionHealthCheckRepository();
        var checker = new FakeConnectivityChecker(_ => new ConnectivityCheckResult(true, 1, "PostgreSQL 16.2", null));

        var service = new ConnectionHealthCheckService(
            connections, healthRepo, new FakePasswordProtector(), checker, new RecordingMetricsRecorder(),
            new FixedClock(DateTimeOffset.UtcNow),
            new FakeSystemSettingsRepository(FakeSystemSettingsRepository.CreateDefault()),
            NullLogger<ConnectionHealthCheckService>.Instance);

        await service.RunOnceAsync();

        Assert.Empty(healthRepo.Results);
    }
}
