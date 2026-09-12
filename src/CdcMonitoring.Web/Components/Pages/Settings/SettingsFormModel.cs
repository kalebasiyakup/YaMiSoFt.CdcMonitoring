using System.ComponentModel.DataAnnotations;

namespace CdcMonitoring.Web.Components.Pages.Settings;

public class SettingsFormModel
{
    [Range(5, 3600)] public int HealthCheckIntervalSeconds { get; set; }
    [Range(1, 100)] public int HealthCheckMaxDegreeOfParallelism { get; set; }
    [Range(1, 60)] public int HealthCheckTimeoutSeconds { get; set; }

    [Range(5, 3600)] public int DiscoveryIntervalSeconds { get; set; }
    [Range(1, 100)] public int DiscoveryMaxDegreeOfParallelism { get; set; }
    [Range(1, 60)] public int DiscoveryTimeoutSeconds { get; set; }

    [Range(5, 3600)] public int AlertingIntervalSeconds { get; set; }
    [Range(1, 1440)] public int SlotInactiveMinutes { get; set; }
    [Range(1, 1440)] public int LagWarningSustainedMinutes { get; set; }
    [Range(0, long.MaxValue)] public long LagWarningBytes { get; set; }
    [Range(1, 20)] public int ConsecutiveHealthCheckFailures { get; set; }
    public string HealthyWalStatuses { get; set; } = string.Empty;

    [Range(1, 365)] public int ReconciliationIntervalDays { get; set; }

    public bool EmailEnabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    [Range(1, 65535)] public int SmtpPort { get; set; } = 587;
    public bool SmtpUseStartTls { get; set; } = true;
    public string? SmtpUsername { get; set; }
    public string? NewSmtpPassword { get; set; }
    public bool HasSmtpPassword { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = string.Empty;
    public string Recipients { get; set; } = string.Empty;
}
