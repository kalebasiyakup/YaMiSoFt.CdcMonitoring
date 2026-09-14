using System.ComponentModel.DataAnnotations;

namespace CdcMonitoring.Web.Components.Pages.Settings;

public class SettingsFormModel
{
    [Range(5, 3600, ErrorMessage = "Aralık 5-3600 sn arasında olmalıdır.")]
    public int HealthCheckIntervalSeconds { get; set; }
    [Range(1, 100, ErrorMessage = "Paralellik 1-100 arasında olmalıdır.")]
    public int HealthCheckMaxDegreeOfParallelism { get; set; }
    [Range(1, 60, ErrorMessage = "Zaman aşımı 1-60 sn arasında olmalıdır.")]
    public int HealthCheckTimeoutSeconds { get; set; }
    [Range(1, 3650, ErrorMessage = "Saklama süresi 1-3650 gün arasında olmalıdır.")]
    public int HealthCheckRetentionDays { get; set; } = 7;

    [Range(5, 3600, ErrorMessage = "Aralık 5-3600 sn arasında olmalıdır.")]
    public int DiscoveryIntervalSeconds { get; set; }
    [Range(1, 100, ErrorMessage = "Paralellik 1-100 arasında olmalıdır.")]
    public int DiscoveryMaxDegreeOfParallelism { get; set; }
    [Range(1, 60, ErrorMessage = "Zaman aşımı 1-60 sn arasında olmalıdır.")]
    public int DiscoveryTimeoutSeconds { get; set; }

    [Range(5, 3600, ErrorMessage = "Aralık 5-3600 sn arasında olmalıdır.")]
    public int AlertingIntervalSeconds { get; set; }
    [Range(1, 1440, ErrorMessage = "Eşik 1-1440 dk arasında olmalıdır.")]
    public int SlotInactiveMinutes { get; set; }
    [Range(1, 1440, ErrorMessage = "Eşik 1-1440 dk arasında olmalıdır.")]
    public int LagWarningSustainedMinutes { get; set; }
    [Range(0, long.MaxValue, ErrorMessage = "Lag eşiği 0 veya üzeri olmalıdır.")]
    public long LagWarningBytes { get; set; }
    [Range(1, 20, ErrorMessage = "Hata sayısı 1-20 arasında olmalıdır.")]
    public int ConsecutiveHealthCheckFailures { get; set; }
    public string HealthyWalStatuses { get; set; } = string.Empty;

    [Range(1, 100000, ErrorMessage = "Sıklık 1 veya üzeri olmalıdır.")]
    public int ReconciliationIntervalValue { get; set; } = 7;
    public ReconciliationIntervalUnit ReconciliationIntervalUnit { get; set; } = ReconciliationIntervalUnit.Days;
    [Range(1, 3650, ErrorMessage = "Saklama süresi 1-3650 gün arasında olmalıdır.")]
    public int ReconciliationRetentionDays { get; set; } = 90;

    public bool SchemaCatalogEnabled { get; set; } = true;
    [Range(1, 100000, ErrorMessage = "Sıklık 1 veya üzeri olmalıdır.")]
    public int SchemaCatalogIntervalValue { get; set; } = 6;
    public ReconciliationIntervalUnit SchemaCatalogIntervalUnit { get; set; } = ReconciliationIntervalUnit.Hours;
    [Range(1, 100, ErrorMessage = "Paralellik 1-100 arasında olmalıdır.")]
    public int SchemaCatalogMaxDegreeOfParallelism { get; set; } = 5;
    [Range(1, 600, ErrorMessage = "Zaman aşımı 1-600 sn arasında olmalıdır.")]
    public int SchemaCatalogTimeoutSeconds { get; set; } = 30;
    public string SchemaCatalogExcludedSchemas { get; set; } = string.Empty;
    [Range(1, 3650, ErrorMessage = "Saklama süresi 1-3650 gün arasında olmalıdır.")]
    public int SchemaChangeRetentionDays { get; set; } = 180;
    [Range(1, 3650, ErrorMessage = "Saklama süresi 1-3650 gün arasında olmalıdır.")]
    public int SchemaScanRetentionDays { get; set; } = 30;

    public bool EmailEnabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    [Range(1, 65535, ErrorMessage = "Port 1-65535 aralığında olmalıdır.")]
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseStartTls { get; set; } = true;
    public string? SmtpUsername { get; set; }
    public string? NewSmtpPassword { get; set; }
    public bool HasSmtpPassword { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = string.Empty;
    public string Recipients { get; set; } = string.Empty;
}

public enum ReconciliationIntervalUnit
{
    Minutes,
    Hours,
    Days
}
