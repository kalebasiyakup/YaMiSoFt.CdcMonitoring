using CdcMonitoring.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace CdcMonitoring.Application.Retention;

/// <summary>
/// Health check, veri tutarlılık kontrolü, şema tarama ve şema değişiklik kayıtlarının
/// sınırsız büyümesini önler (audit log kasıtlı olarak kapsam dışıdır — o kalıcı tutulmalı).
/// Katalogun kendisi (tablo/kolon/indeks/kısıt) de kapsam dışıdır: o geçmiş değil güncel
/// durumdur, silinirse ekran boşalır. Saklama süreleri Ayarlar ekranından (SystemSettings)
/// yönetilir.
/// </summary>
public class RetentionCleanupService(
    ISystemSettingsRepository settingsRepository,
    ICdcRelationshipHealthRepository healthRepository,
    IReconciliationResultRepository reconciliationResultRepository,
    ISchemaCatalogRepository schemaCatalogRepository,
    ISchemaChangeEventRepository schemaChangeEventRepository,
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
        var deletedSchemaScans = await schemaCatalogRepository.DeleteScansOlderThanAsync(
            now.AddDays(-settings.SchemaScanRetentionDays), ct);
        var deletedSchemaChanges = await schemaChangeEventRepository.DeleteOlderThanAsync(
            now.AddDays(-settings.SchemaChangeRetentionDays), ct);

        if (deletedHealthEntries > 0 || deletedReconciliationResults > 0 || deletedSchemaScans > 0 || deletedSchemaChanges > 0)
        {
            logger.LogInformation(
                "Retention temizliği: {HealthEntries} health check kaydı, {ReconciliationResults} veri tutarlılık kontrolü kaydı, " +
                "{SchemaScans} şema tarama kaydı, {SchemaChanges} şema değişiklik kaydı silindi.",
                deletedHealthEntries, deletedReconciliationResults, deletedSchemaScans, deletedSchemaChanges);
        }
    }
}
