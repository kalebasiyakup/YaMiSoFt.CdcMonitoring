using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class CdcRelationshipHealthConfiguration : IEntityTypeConfiguration<CdcRelationshipHealth>
{
    public void Configure(EntityTypeBuilder<CdcRelationshipHealth> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Bir CdcRelationship için periyodik sağlık anlık görüntüsü (slot aktifliği, WAL " +
            "durumu, lag, subscription durumu). Topoloji ekranındaki kenar renklerinin ve " +
            "slot/lag/subscription alarmlarının kaynağıdır."));

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasComment("Sağlık kaydının birincil anahtarı.");
        builder.Property(h => h.RelationshipId).HasComment("Bağlı olduğu CdcRelationship.Id.");
        builder.Property(h => h.CheckedAt).HasComment("Bu anlık görüntünün alındığı zaman.");
        builder.Property(h => h.SlotActive).HasComment("pg_replication_slots.active değeri; slot'un o an aktif olup olmadığı.");
        builder.Property(h => h.WalStatus).HasMaxLength(50)
            .HasComment("pg_replication_slots.wal_status değeri (ör. reserved, extended, lost).");
        builder.Property(h => h.LagBytes).HasComment("Kaynak-hedef arasındaki replikasyon gecikmesi, byte cinsinden.");
        builder.Property(h => h.SubscriptionState).HasConversion<string>().HasMaxLength(20)
            .HasComment("Subscription durumu: Enabled=çalışıyor, Disabled=devre dışı, " +
                "Error=hata veriyor, Unknown=okunamadı.");
        builder.Property(h => h.LastSyncAt).HasComment("Subscription'ın en son başarılı senkronizasyon zamanı.");

        builder.HasIndex(h => new { h.RelationshipId, h.CheckedAt });

        // Retention temizliğinin (ilişkiden bağımsız, salt CheckedAt < cutoff) tüm tabloyu
        // taramadan çalışabilmesi için ayrı bir indeks.
        builder.HasIndex(h => h.CheckedAt);
    }
}
