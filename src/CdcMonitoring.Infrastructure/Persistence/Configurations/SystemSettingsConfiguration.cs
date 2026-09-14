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
        builder.ToTable(t => t.HasComment(
            "Tekil (sabit Id'li) ayar satırı: tarama aralıkları, alarm eşikleri, SMTP/e-posta " +
            "yapılandırması ve saklama süreleri. /settings ekranından yeniden başlatma " +
            "gerekmeden düzenlenir."));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasComment("Sabit singleton Id (00000000-0000-0000-0000-000000000001).");
        builder.Property(s => s.HealthCheckIntervalSeconds).HasComment("ConnectionHealthCheck taramaları arasındaki süre (sn).");
        builder.Property(s => s.HealthCheckMaxDegreeOfParallelism).HasComment("Health check taramasında eşzamanlı çalışacak bağlantı sayısı.");
        builder.Property(s => s.HealthCheckTimeoutSeconds).HasComment("Bir health check denemesi için zaman aşımı süresi (sn).");
        builder.Property(s => s.HealthCheckRetentionDays).HasComment("ConnectionHealthCheck kayıtlarının saklanma süresi (gün).");
        builder.Property(s => s.DiscoveryIntervalSeconds).HasComment("CDC ilişki keşif (discovery) taramaları arasındaki süre (sn).");
        builder.Property(s => s.DiscoveryMaxDegreeOfParallelism).HasComment("Keşif taramasında eşzamanlı çalışacak bağlantı sayısı.");
        builder.Property(s => s.DiscoveryTimeoutSeconds).HasComment("Bir keşif denemesi için zaman aşımı süresi (sn).");
        builder.Property(s => s.AlertingIntervalSeconds).HasComment("Alarm değerlendirme (eşik kontrolü) taramaları arasındaki süre (sn).");
        builder.Property(s => s.SlotInactiveMinutes).HasComment("Bir slot'un bu süre boyunca inaktif kalması SlotInactive alarmı üretir (dk).");
        builder.Property(s => s.LagWarningSustainedMinutes).HasComment("Lag eşiğinin bu süre boyunca sürekli aşılması LagWarning alarmı üretir (dk).");
        builder.Property(s => s.LagWarningBytes).HasComment("LagWarning alarmı için byte cinsinden eşik değer.");
        builder.Property(s => s.ConsecutiveHealthCheckFailures).HasComment("HealthCheckFailed alarmı üretmek için gereken ardışık başarısız kontrol sayısı.");
        builder.Property(s => s.HealthyWalStatusesCsv).HasMaxLength(200).IsRequired()
            .HasComment("Sağlıklı sayılan pg_replication_slots.wal_status değerlerinin virgülle ayrılmış listesi (ör. reserved,extended).");
        builder.Property(s => s.ReconciliationIntervalSeconds).HasComment("Veri Tutarlılık Kontrolü taramaları arasındaki süre (sn).");
        builder.Property(s => s.ReconciliationRetentionDays).HasComment("ReconciliationResult kayıtlarının saklanma süresi (gün).");
        builder.Property(s => s.SchemaCatalogEnabled)
            .HasComment("false ise periyodik şema katalog taraması hiç çalışmaz; katalog ekranından elle tarama yapılabilir.");
        builder.Property(s => s.SchemaCatalogIntervalSeconds).HasComment("Şema katalog taramaları arasındaki süre (sn).");
        builder.Property(s => s.SchemaCatalogMaxDegreeOfParallelism).HasComment("Katalog taramasında eşzamanlı çalışacak bağlantı sayısı.");
        builder.Property(s => s.SchemaCatalogTimeoutSeconds).HasComment("Bir bağlantının katalog taraması için zaman aşımı süresi (sn).");
        builder.Property(s => s.SchemaCatalogExcludedSchemasCsv).HasMaxLength(500).IsRequired()
            .HasComment("Katalog taramasının dışladığı şemaların virgülle ayrılmış listesi (ör. pg_catalog,information_schema,pg_toast).");
        builder.Property(s => s.SchemaChangeRetentionDays).HasComment("SchemaChangeEvent kayıtlarının saklanma süresi (gün).");
        builder.Property(s => s.SchemaScanRetentionDays).HasComment("SchemaScan (tarama geçmişi) kayıtlarının saklanma süresi (gün).");
        builder.Property(s => s.EmailEnabled).HasComment("E-posta bildirimlerinin açık olup olmadığı.");
        builder.Property(s => s.SmtpHost).HasMaxLength(255)
            .HasComment("SMTP sunucu adresi.");
        builder.Property(s => s.SmtpPort).HasComment("SMTP sunucu portu.");
        builder.Property(s => s.SmtpUseStartTls).HasComment("SMTP bağlantısında STARTTLS kullanılıp kullanılmayacağı.");
        builder.Property(s => s.SmtpUsername).HasMaxLength(200)
            .HasComment("SMTP kimlik doğrulama kullanıcı adı.");
        builder.Property(s => s.EncryptedSmtpPassword).HasColumnType("text")
            .HasComment("Data Protection ile şifrelenmiş SMTP parolası; hiçbir ekranda geri gösterilmez.");
        builder.Property(s => s.FromAddress).HasMaxLength(255).IsRequired()
            .HasComment("Bildirim e-postalarının gönderen adresi.");
        builder.Property(s => s.FromDisplayName).HasMaxLength(200).IsRequired()
            .HasComment("Bildirim e-postalarının gönderen görünen adı.");
        builder.Property(s => s.RecipientsCsv).HasColumnType("text")
            .HasComment("Bildirim e-postalarının alıcı listesi, virgülle ayrılmış.");
        builder.Property(s => s.UpdatedAt).HasComment("Ayarların son güncellenme zamanı.");
        builder.Property(s => s.UpdatedBy).HasMaxLength(200)
            .HasComment("Ayarları son güncelleyen kullanıcı.");

        builder.HasData(new SystemSettings
        {
            Id = SingletonId,
            HealthCheckIntervalSeconds = 30,
            HealthCheckMaxDegreeOfParallelism = 10,
            HealthCheckTimeoutSeconds = 5,
            HealthCheckRetentionDays = 7,
            DiscoveryIntervalSeconds = 60,
            DiscoveryMaxDegreeOfParallelism = 5,
            DiscoveryTimeoutSeconds = 10,
            AlertingIntervalSeconds = 60,
            SlotInactiveMinutes = 5,
            LagWarningSustainedMinutes = 10,
            LagWarningBytes = 50 * 1024 * 1024,
            ConsecutiveHealthCheckFailures = 2,
            HealthyWalStatusesCsv = "reserved,extended",
            ReconciliationIntervalSeconds = 7 * 86400,
            ReconciliationRetentionDays = 90,
            SchemaCatalogEnabled = true,
            SchemaCatalogIntervalSeconds = 6 * 3600,
            SchemaCatalogMaxDegreeOfParallelism = 5,
            SchemaCatalogTimeoutSeconds = 30,
            SchemaCatalogExcludedSchemasCsv = "pg_catalog,information_schema,pg_toast",
            SchemaChangeRetentionDays = 180,
            SchemaScanRetentionDays = 30,
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
