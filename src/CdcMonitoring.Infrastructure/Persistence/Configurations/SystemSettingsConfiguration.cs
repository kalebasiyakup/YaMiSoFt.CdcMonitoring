using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    // Tek satırlık (singleton) ayar kaydı için sabit Id — migration seed'inde kullanılır.
    public static readonly Guid SingletonId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.HealthyWalStatusesCsv).HasMaxLength(200).IsRequired();
        builder.Property(s => s.SmtpHost).HasMaxLength(255);
        builder.Property(s => s.SmtpUsername).HasMaxLength(200);
        builder.Property(s => s.EncryptedSmtpPassword).HasColumnType("text");
        builder.Property(s => s.FromAddress).HasMaxLength(255).IsRequired();
        builder.Property(s => s.FromDisplayName).HasMaxLength(200).IsRequired();
        builder.Property(s => s.RecipientsCsv).HasColumnType("text");
        builder.Property(s => s.UpdatedBy).HasMaxLength(200);

        builder.HasData(new SystemSettings
        {
            Id = SingletonId,
            HealthCheckIntervalSeconds = 30,
            HealthCheckMaxDegreeOfParallelism = 10,
            HealthCheckTimeoutSeconds = 5,
            DiscoveryIntervalSeconds = 60,
            DiscoveryMaxDegreeOfParallelism = 5,
            DiscoveryTimeoutSeconds = 10,
            AlertingIntervalSeconds = 60,
            SlotInactiveMinutes = 5,
            LagWarningSustainedMinutes = 10,
            LagWarningBytes = 50 * 1024 * 1024,
            ConsecutiveHealthCheckFailures = 2,
            HealthyWalStatusesCsv = "reserved,extended",
            ReconciliationIntervalDays = 7,
            EmailEnabled = false,
            SmtpHost = string.Empty,
            SmtpPort = 587,
            SmtpUseStartTls = true,
            SmtpUsername = null,
            EncryptedSmtpPassword = null,
            FromAddress = "cdc-monitoring@example.com",
            FromDisplayName = "CDC Monitoring",
            RecipientsCsv = string.Empty,
            UpdatedAt = DateTimeOffset.UnixEpoch,
            UpdatedBy = null
        });
    }
}
