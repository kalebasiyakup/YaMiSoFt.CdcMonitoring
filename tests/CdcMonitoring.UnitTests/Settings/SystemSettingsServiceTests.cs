using CdcMonitoring.Application.Settings;
using CdcMonitoring.UnitTests.TestDoubles;
using Xunit;

namespace CdcMonitoring.UnitTests.Settings;

public class SystemSettingsServiceTests
{
    private static (SystemSettingsService Service, InMemoryAuditLogRepository AuditLog) CreateService()
    {
        var repo = new FakeSystemSettingsRepository(FakeSystemSettingsRepository.CreateDefault());
        var auditLog = new InMemoryAuditLogRepository();
        var service = new SystemSettingsService(
            repo, new FakeSmtpPasswordProtector(), new FakeEmailNotifier(), auditLog,
            new FixedCurrentUserAccessor("yakup.kalebasi"),
            new FixedClock(DateTimeOffset.Parse("2026-09-12T10:00:00Z")));

        return (service, auditLog);
    }

    private static UpdateSystemSettingsRequest ValidRequest(string? newSmtpPassword = "s3cr3t!") => new(
        HealthCheckIntervalSeconds: 45,
        HealthCheckMaxDegreeOfParallelism: 8,
        HealthCheckTimeoutSeconds: 6,
        HealthCheckRetentionDays: 14,
        DiscoveryIntervalSeconds: 90,
        DiscoveryMaxDegreeOfParallelism: 4,
        DiscoveryTimeoutSeconds: 12,
        AlertingIntervalSeconds: 45,
        SlotInactiveMinutes: 3,
        LagWarningSustainedMinutes: 8,
        LagWarningBytes: 10_000_000,
        ConsecutiveHealthCheckFailures: 3,
        HealthyWalStatuses: ["reserved"],
        ReconciliationIntervalSeconds: 14 * 86400,
        ReconciliationRetentionDays: 120,
        SchemaCatalogEnabled: false,
        SchemaCatalogIntervalSeconds: 3 * 3600,
        SchemaCatalogMaxDegreeOfParallelism: 3,
        SchemaCatalogTimeoutSeconds: 45,
        SchemaCatalogExcludedSchemas: ["pg_catalog", "information_schema", "pg_toast", "audit"],
        SchemaChangeRetentionDays: 200,
        SchemaScanRetentionDays: 45,
        EmailEnabled: true,
        SmtpHost: "smtp.example.com",
        SmtpPort: 25,
        SmtpUseStartTls: false,
        SmtpUsername: "notifier",
        NewSmtpPassword: newSmtpPassword,
        FromAddress: "alerts@example.com",
        FromDisplayName: "Alerts",
        Recipients: ["a@example.com", "b@example.com"]);

    [Fact]
    public async Task GetAsync_returns_defaults_seeded_by_repository()
    {
        var (service, _) = CreateService();

        var dto = await service.GetAsync();

        Assert.Equal(30, dto.HealthCheckIntervalSeconds);
        Assert.False(dto.HasSmtpPassword);
    }

    [Fact]
    public async Task UpdateAsync_persists_all_fields_and_writes_audit_log_without_password()
    {
        var (service, auditLog) = CreateService();

        var updated = await service.UpdateAsync(ValidRequest());

        Assert.Equal(45, updated.HealthCheckIntervalSeconds);
        Assert.Equal(8, updated.HealthCheckMaxDegreeOfParallelism);
        Assert.Equal(90, updated.DiscoveryIntervalSeconds);
        Assert.Equal(3, updated.SlotInactiveMinutes);
        Assert.Equal(14 * 86400, updated.ReconciliationIntervalSeconds);
        Assert.Equal(14, updated.HealthCheckRetentionDays);
        Assert.Equal(120, updated.ReconciliationRetentionDays);
        Assert.False(updated.SchemaCatalogEnabled);
        Assert.Equal(3 * 3600, updated.SchemaCatalogIntervalSeconds);
        Assert.Equal(45, updated.SchemaCatalogTimeoutSeconds);
        Assert.Equal(["pg_catalog", "information_schema", "pg_toast", "audit"], updated.SchemaCatalogExcludedSchemas);
        Assert.Equal(200, updated.SchemaChangeRetentionDays);
        Assert.Equal(45, updated.SchemaScanRetentionDays);
        Assert.True(updated.EmailEnabled);
        Assert.Equal(["a@example.com", "b@example.com"], updated.Recipients);
        Assert.Equal(["reserved"], updated.HealthyWalStatuses);
        Assert.True(updated.HasSmtpPassword);
        Assert.Equal("yakup.kalebasi", updated.UpdatedBy);

        var auditEntry = Assert.Single(auditLog.Entries);
        Assert.DoesNotContain("s3cr3t!", auditEntry.NewValue);
        Assert.DoesNotContain("enc:", auditEntry.NewValue);
    }

    [Fact]
    public async Task UpdateAsync_without_new_password_keeps_existing_encrypted_password()
    {
        var (service, _) = CreateService();
        await service.UpdateAsync(ValidRequest());

        var afterSecondUpdate = await service.UpdateAsync(ValidRequest(newSmtpPassword: null) with { FromDisplayName = "Alerts v2" });

        Assert.True(afterSecondUpdate.HasSmtpPassword);
        Assert.Equal("Alerts v2", afterSecondUpdate.FromDisplayName);
    }
}
