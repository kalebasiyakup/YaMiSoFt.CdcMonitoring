namespace CdcMonitoring.Domain.Entities;

public class SystemSettings
{
    public Guid Id { get; set; }

    public int HealthCheckIntervalSeconds { get; set; } = 30;
    public int HealthCheckMaxDegreeOfParallelism { get; set; } = 10;
    public int HealthCheckTimeoutSeconds { get; set; } = 5;
    public int HealthCheckRetentionDays { get; set; } = 7;

    public int DiscoveryIntervalSeconds { get; set; } = 60;
    public int DiscoveryMaxDegreeOfParallelism { get; set; } = 5;
    public int DiscoveryTimeoutSeconds { get; set; } = 10;

    public int AlertingIntervalSeconds { get; set; } = 60;
    public int SlotInactiveMinutes { get; set; } = 5;
    public int LagWarningSustainedMinutes { get; set; } = 10;
    public long LagWarningBytes { get; set; } = 50 * 1024 * 1024;
    public int ConsecutiveHealthCheckFailures { get; set; } = 2;
    public required string HealthyWalStatusesCsv { get; set; }

    public int ReconciliationIntervalSeconds { get; set; } = 7 * 86400;
    public int ReconciliationRetentionDays { get; set; } = 90;

    public bool EmailEnabled { get; set; }
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public bool SmtpUseStartTls { get; set; } = true;
    public string? SmtpUsername { get; set; }
    public string? EncryptedSmtpPassword { get; set; }
    public required string FromAddress { get; set; }
    public required string FromDisplayName { get; set; }
    public string RecipientsCsv { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}
