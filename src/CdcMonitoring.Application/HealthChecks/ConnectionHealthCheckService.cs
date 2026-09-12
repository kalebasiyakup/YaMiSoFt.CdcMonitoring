using System.Diagnostics;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CdcMonitoring.Application.HealthChecks;

public class ConnectionHealthCheckService(
    IPgConnectionRepository connections,
    IConnectionHealthCheckRepository healthCheckRepository,
    IConnectionPasswordProtector passwordProtector,
    IPostgresConnectivityChecker connectivityChecker,
    IMetricsRecorder metrics,
    IClock clock,
    ISystemSettingsRepository settingsRepository,
    ILogger<ConnectionHealthCheckService> logger)
{
    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var settings = await settingsRepository.GetAsync(ct);
        var active = await connections.GetActiveAsync(ct);
        using var throttle = new SemaphoreSlim(Math.Max(1, settings.HealthCheckMaxDegreeOfParallelism));

        var tasks = active.Select(async connection =>
        {
            await throttle.WaitAsync(ct);
            try
            {
                await CheckOneAsync(connection, settings.HealthCheckTimeoutSeconds, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Bağlantı health check sırasında beklenmeyen hata: {ConnectionName}", connection.Name);
            }
            finally
            {
                throttle.Release();
            }
        });

        await Task.WhenAll(tasks);
        await healthCheckRepository.SaveChangesAsync(ct);

        stopwatch.Stop();
        metrics.RecordScanCycleDuration("connection_health_check", stopwatch.Elapsed.TotalSeconds);
    }

    private async Task CheckOneAsync(PgConnection connection, int timeoutSeconds, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var plaintextPassword = passwordProtector.Unprotect(connection.EncryptedPassword);
        var result = await connectivityChecker.CheckAsync(connection, plaintextPassword, timeoutCts.Token);

        await healthCheckRepository.AddAsync(new ConnectionHealthCheck
        {
            Id = Guid.NewGuid(),
            ConnectionId = connection.Id,
            CheckedAt = clock.UtcNow,
            IsUp = result.IsUp,
            LatencyMs = result.LatencyMs,
            PostgresVersion = result.PostgresVersion,
            ErrorMessage = result.ErrorMessage
        }, ct);

        metrics.RecordConnectionHealth(connection.Name, connection.EnvironmentTag, result.IsUp, result.LatencyMs);

        if (!result.IsUp)
            logger.LogWarning("Bağlantı erişilemez durumda: {ConnectionName} ({Host}:{Port}) — {Error}", connection.Name, connection.Host, connection.Port, result.ErrorMessage);
    }
}
