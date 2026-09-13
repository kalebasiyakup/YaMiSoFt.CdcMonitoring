using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Alerting;
using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Application.HealthChecks;
using CdcMonitoring.Application.Reconciliation;
using CdcMonitoring.Application.Retention;
using Microsoft.Extensions.Logging;
using Quartz;

namespace CdcMonitoring.Infrastructure.Jobs;

/// <summary>
/// Sabit, kısa aralıklarla (bkz. DependencyInjection) "yoklama" yapan tek bir job.
/// Her alt işin (health check/discovery/alerting/reconciliation) gerçek çalışma
/// sıklığı DB'deki SystemSettings'ten okunur ve UI'dan değiştirilince yeniden
/// başlatma gerekmeden bir sonraki yoklamada devreye girer. Aynı işin birden
/// fazla replikada aynı anda çalışmasını, IJobScheduleRepository.TryClaimAsync
/// içindeki atomik koşullu UPDATE önler (NFR-05).
/// </summary>
[DisallowConcurrentExecution]
public class SchedulerTickJob(
    ISystemSettingsRepository settingsRepository,
    IJobScheduleRepository jobScheduleRepository,
    ConnectionHealthCheckService healthCheckService,
    CdcDiscoveryService discoveryService,
    AlertEvaluationService alertEvaluationService,
    ReconciliationService reconciliationService,
    RetentionCleanupService retentionCleanupService,
    IClock clock,
    ILogger<SchedulerTickJob> logger) : IJob
{
    public const string Key = "scheduler-tick";

    // Retention temizliği gün bazlı saklama sürelerine göre çalıştığı için dakika/saniye
    // hassasiyetinde bir kullanıcı ayarına ihtiyaç yok — günde bir taraması yeterli.
    private static readonly TimeSpan RetentionCleanupInterval = TimeSpan.FromHours(24);

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var settings = await settingsRepository.GetAsync(ct);
        var now = clock.UtcNow;

        await RunIfDueAsync(JobNames.ConnectionHealthCheck, TimeSpan.FromSeconds(settings.HealthCheckIntervalSeconds), now,
            () => healthCheckService.RunOnceAsync(ct), ct);

        await RunIfDueAsync(JobNames.CdcDiscovery, TimeSpan.FromSeconds(settings.DiscoveryIntervalSeconds), now,
            () => discoveryService.RunOnceAsync(ct), ct);

        await RunIfDueAsync(JobNames.AlertEvaluation, TimeSpan.FromSeconds(settings.AlertingIntervalSeconds), now,
            () => alertEvaluationService.RunOnceAsync(ct), ct);

        await RunIfDueAsync(JobNames.WeeklyReconciliation, TimeSpan.FromSeconds(settings.ReconciliationIntervalSeconds), now,
            () => reconciliationService.RunOnceAsync(ct), ct);

        await RunIfDueAsync(JobNames.RetentionCleanup, RetentionCleanupInterval, now,
            () => retentionCleanupService.RunOnceAsync(ct), ct);
    }

    private async Task RunIfDueAsync(string jobName, TimeSpan interval, DateTimeOffset now, Func<Task> action, CancellationToken ct)
    {
        if (!await jobScheduleRepository.TryClaimAsync(jobName, interval, now, ct))
            return;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Job başarısız oldu: {JobName}", jobName);

            // Claim, iş çalıştırılmadan ÖNCE alındığı için (yukarıdaki atomik UPDATE), başarısız
            // bir çalıştırma serbest bırakılmazsa bir sonraki deneme tam bir interval sonrasına
            // kadar ertelenir. Burada serbest bırakmak, geçici hatalardan sonra bir sonraki
            // 15sn'lik yoklamada tekrar denenmesini sağlar.
            await jobScheduleRepository.ReleaseClaimAsync(jobName, ct);
        }
    }
}
