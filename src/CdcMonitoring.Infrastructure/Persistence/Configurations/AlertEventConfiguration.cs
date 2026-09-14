using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class AlertEventConfiguration : IEntityTypeConfiguration<AlertEvent>
{
    public void Configure(EntityTypeBuilder<AlertEvent> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Eşik aşımlarında açılan alarm kayıtları; anti-flap mantığıyla aynı aktif sorun " +
            "için tekrar bildirim göndermez, durum düzelince ResolvedAt ile otomatik kapanır."));

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasComment("Alarm kaydının birincil anahtarı.");
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(40)
            .HasComment("Alarm türü: SlotInactive, WalCritical, SubscriptionError, " +
                "LagWarning, HealthCheckFailed, ReconciliationMismatch.");
        builder.Property(a => a.Severity).HasConversion<string>().HasMaxLength(20)
            .HasComment("Önem derecesi: Info, Warning, Critical.");
        builder.Property(a => a.ConnectionId).HasComment("İlgili PgConnection.Id (bağlantı düzeyinde alarmlarda dolu).");
        builder.Property(a => a.RelationshipId).HasComment("İlgili CdcRelationship.Id (ilişki düzeyinde alarmlarda dolu).");
        builder.Property(a => a.TriggeredAt).HasComment("Alarmın ilk tetiklendiği zaman.");
        builder.Property(a => a.ResolvedAt).HasComment("Sorunun düzeldiği zaman; null ise alarm hâlâ aktiftir.");
        builder.Property(a => a.NotifiedAt).HasComment("E-posta bildiriminin gönderildiği zaman; null ise henüz bildirilmedi.");
        builder.Property(a => a.Message).HasMaxLength(2000).IsRequired()
            .HasComment("Kullanıcıya gösterilen/e-postaya yazılan alarm mesajı.");
        builder.Ignore(a => a.IsActive);
        builder.HasIndex(a => new { a.ConnectionId, a.RelationshipId, a.ResolvedAt });
    }
}
