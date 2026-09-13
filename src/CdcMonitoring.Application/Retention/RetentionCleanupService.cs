using CdcMonitoring.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CdcMonitoring.Application.Retention;

/// <summary>
/// Health check ve veri tutarlılık kontrolü kayıtlarının sınırsız büyümesini önler
/// (audit log kasıtlı olarak kapsam dışıdır — o kalıcı tutulmalı). Saklama süreleri
/// Ayarlar ekranından (SystemSettings) yönetilir.
/// </summary>
public class RetentionCleanupService(
    ISystemSettingsRepository settingsRepository,
    ICdcRelationshipHealthRepository healthRepository,
    IReconciliationResultRepository reconciliationResultRepository,
    IClock clock,
    ILogger<RetentionCleanupService> logger)
{
    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        var settings = await settingsRepository.GetAsync(ct);
        var now = clock.UtcNow;

        var deletedHealthEntries = await healthRepository.DeleteOlderThanAsync(
            now.AddDays(-settings.HealthCheckRetentionDays), ct);
        var deletedReconciliationResults = await reconciliationResultRepository.DeleteOlderThanAsync(
            now.AddDays(-settings.ReconciliationRetentionDays), ct);

        if (deletedHealthEntries > 0 || deletedReconciliationResults > 0)
        {
            logger.LogInformation(
                "Retention temizliği: {HealthEntries} health check kaydı, {ReconciliationResults} veri tutarlılık kontrolü kaydı silindi.",
                deletedHealthEntries, deletedReconciliationResults);
        }
    }
}
