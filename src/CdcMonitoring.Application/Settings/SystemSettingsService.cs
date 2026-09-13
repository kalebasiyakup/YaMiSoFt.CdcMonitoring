using System.Text.Json;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Common;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.Settings;

public class SystemSettingsService(
    ISystemSettingsRepository settings,
    ISmtpPasswordProtector smtpPasswordProtector,
    IEmailNotifier emailNotifier,
    IAuditLogRepository auditLog,
    ICurrentUserAccessor currentUser,
    IClock clock)
{
    public async Task<SystemSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var s = await settings.GetAsync(ct);
        return ToDto(s);
    }

    public async Task<SystemSettingsDto> UpdateAsync(UpdateSystemSettingsRequest request, CancellationToken ct = default)
    {
        var s = await settings.GetAsync(ct);
        var actor = currentUser.GetCurrentUserNameOrDefault();
        var now = clock.UtcNow;
        var before = ToAuditSnapshot(s);

        s.HealthCheckIntervalSeconds = request.HealthCheckIntervalSeconds;
        s.HealthCheckMaxDegreeOfParallelism = request.HealthCheckMaxDegreeOfParallelism;
        s.HealthCheckTimeoutSeconds = request.HealthCheckTimeoutSeconds;
        s.HealthCheckRetentionDays = request.HealthCheckRetentionDays;
        s.DiscoveryIntervalSeconds = request.DiscoveryIntervalSeconds;
        s.DiscoveryMaxDegreeOfParallelism = request.DiscoveryMaxDegreeOfParallelism;
        s.DiscoveryTimeoutSeconds = request.DiscoveryTimeoutSeconds;
        s.AlertingIntervalSeconds = request.AlertingIntervalSeconds;
        s.SlotInactiveMinutes = request.SlotInactiveMinutes;
        s.LagWarningSustainedMinutes = request.LagWarningSustainedMinutes;
        s.LagWarningBytes = request.LagWarningBytes;
        s.ConsecutiveHealthCheckFailures = request.ConsecutiveHealthCheckFailures;
        s.HealthyWalStatusesCsv = string.Join(",", request.HealthyWalStatuses);
        s.ReconciliationIntervalSeconds = request.ReconciliationIntervalSeconds;
        s.ReconciliationRetentionDays = request.ReconciliationRetentionDays;
        s.EmailEnabled = request.EmailEnabled;
        s.SmtpHost = request.SmtpHost;
        s.SmtpPort = request.SmtpPort;
        s.SmtpUseStartTls = request.SmtpUseStartTls;
        s.SmtpUsername = request.SmtpUsername;
        s.FromAddress = request.FromAddress;
        s.FromDisplayName = request.FromDisplayName;
        s.RecipientsCsv = string.Join(",", request.Recipients);
        s.UpdatedAt = now;
        s.UpdatedBy = actor;

        if (!string.IsNullOrWhiteSpace(request.NewSmtpPassword))
            s.EncryptedSmtpPassword = smtpPasswordProtector.Protect(request.NewSmtpPassword);

        await settings.SaveChangesAsync(ct);

        await auditLog.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(SystemSettings),
            EntityId = s.Id,
            Action = AuditAction.Updated,
            ChangedBy = actor,
            ChangedAt = now,
            OldValue = JsonSerializer.Serialize(before),
            NewValue = JsonSerializer.Serialize(ToAuditSnapshot(s))
        }, ct);

        return ToDto(s);
    }

    // Kaydetmeden önce (Ayarlar > E-posta ekranı) SMTP yapılandırmasını gerçek bir test
    // e-postasıyla doğrulamak için (FR-10 ile aynı prensip: FR-03'ün bağlantı testi
    // eşdeğeri). Kalıcı hiçbir kayıt oluşturmaz.
    public async Task<EmailTestResult> TestEmailAsync(TestEmailRequest request, CancellationToken ct = default)
    {
        string password;
        if (!string.IsNullOrWhiteSpace(request.NewSmtpPassword))
        {
            password = request.NewSmtpPassword;
        }
        else
        {
            var current = await settings.GetAsync(ct);
            password = string.IsNullOrEmpty(current.EncryptedSmtpPassword)
                ? string.Empty
                : smtpPasswordProtector.Unprotect(current.EncryptedSmtpPassword);
        }

        return await emailNotifier.SendTestEmailAsync(new EmailTestRequest(
            request.SmtpHost,
            request.SmtpPort,
            request.SmtpUseStartTls,
            request.SmtpUsername,
            password,
            request.FromAddress,
            request.FromDisplayName,
            request.Recipients), ct);
    }

    // SmtpPassword bilinçli olarak dışarıda bırakılır: audit kaydı hiçbir zaman
    // düz metin veya şifreli parolayı içermemelidir (FR-02 ile aynı prensip).
    private static object ToAuditSnapshot(SystemSettings s) => new
    {
        s.HealthCheckIntervalSeconds,
        s.HealthCheckMaxDegreeOfParallelism,
        s.HealthCheckTimeoutSeconds,
        s.HealthCheckRetentionDays,
        s.DiscoveryIntervalSeconds,
        s.DiscoveryMaxDegreeOfParallelism,
        s.DiscoveryTimeoutSeconds,
        s.AlertingIntervalSeconds,
        s.SlotInactiveMinutes,
        s.LagWarningSustainedMinutes,
        s.LagWarningBytes,
        s.ConsecutiveHealthCheckFailures,
        s.HealthyWalStatusesCsv,
        s.ReconciliationIntervalSeconds,
        s.ReconciliationRetentionDays,
        s.EmailEnabled,
        s.SmtpHost,
        s.SmtpPort,
        s.SmtpUseStartTls,
        s.SmtpUsername,
        s.FromAddress,
        s.FromDisplayName,
        s.RecipientsCsv
    };

    private static SystemSettingsDto ToDto(SystemSettings s) => new(
        s.HealthCheckIntervalSeconds,
        s.HealthCheckMaxDegreeOfParallelism,
        s.HealthCheckTimeoutSeconds,
        s.HealthCheckRetentionDays,
        s.DiscoveryIntervalSeconds,
        s.DiscoveryMaxDegreeOfParallelism,
        s.DiscoveryTimeoutSeconds,
        s.AlertingIntervalSeconds,
        s.SlotInactiveMinutes,
        s.LagWarningSustainedMinutes,
        s.LagWarningBytes,
        s.ConsecutiveHealthCheckFailures,
        SplitCsv(s.HealthyWalStatusesCsv),
        s.ReconciliationIntervalSeconds,
        s.ReconciliationRetentionDays,
        s.EmailEnabled,
        s.SmtpHost,
        s.SmtpPort,
        s.SmtpUseStartTls,
        s.SmtpUsername,
        !string.IsNullOrEmpty(s.EncryptedSmtpPassword),
        s.FromAddress,
        s.FromDisplayName,
        SplitCsv(s.RecipientsCsv),
        s.UpdatedAt,
        s.UpdatedBy);

    private static List<string> SplitCsv(string csv) =>
        csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}
