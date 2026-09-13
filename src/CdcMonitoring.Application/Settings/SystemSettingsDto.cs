namespace CdcMonitoring.Application.Settings;

public record SystemSettingsDto(
    int HealthCheckIntervalSeconds,
    int HealthCheckMaxDegreeOfParallelism,
    int HealthCheckTimeoutSeconds,
    int HealthCheckRetentionDays,
    int DiscoveryIntervalSeconds,
    int DiscoveryMaxDegreeOfParallelism,
    int DiscoveryTimeoutSeconds,
    int AlertingIntervalSeconds,
    int SlotInactiveMinutes,
    int LagWarningSustainedMinutes,
    long LagWarningBytes,
    int ConsecutiveHealthCheckFailures,
    List<string> HealthyWalStatuses,
    int ReconciliationIntervalSeconds,
    int ReconciliationRetentionDays,
    bool EmailEnabled,
    string SmtpHost,
    int SmtpPort,
    bool SmtpUseStartTls,
    string? SmtpUsername,
    bool HasSmtpPassword,
    string FromAddress,
    string FromDisplayName,
    List<string> Recipients,
    DateTimeOffset UpdatedAt,
    string? UpdatedBy);

public record UpdateSystemSettingsRequest(
    int HealthCheckIntervalSeconds,
    int HealthCheckMaxDegreeOfParallelism,
    int HealthCheckTimeoutSeconds,
    int HealthCheckRetentionDays,
    int DiscoveryIntervalSeconds,
    int DiscoveryMaxDegreeOfParallelism,
    int DiscoveryTimeoutSeconds,
    int AlertingIntervalSeconds,
    int SlotInactiveMinutes,
    int LagWarningSustainedMinutes,
    long LagWarningBytes,
    int ConsecutiveHealthCheckFailures,
    List<string> HealthyWalStatuses,
    int ReconciliationIntervalSeconds,
    int ReconciliationRetentionDays,
    bool EmailEnabled,
    string SmtpHost,
    int SmtpPort,
    bool SmtpUseStartTls,
    string? SmtpUsername,
    string? NewSmtpPassword,
    string FromAddress,
    string FromDisplayName,
    List<string> Recipients);

// Kaydetmeden önce (Ayarlar > E-posta) mevcut form değerleriyle test e-postası göndermek için.
// NewSmtpPassword boşsa ve daha önce bir parola kaydedilmişse, o parola kullanılır
// (Connections/TestConnectionRequest'teki ExistingConnectionId düşüşüyle aynı prensip).
public record TestEmailRequest(
    string SmtpHost,
    int SmtpPort,
    bool SmtpUseStartTls,
    string? SmtpUsername,
    string? NewSmtpPassword,
    string FromAddress,
    string FromDisplayName,
    List<string> Recipients);
