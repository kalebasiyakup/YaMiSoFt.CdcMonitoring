using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class CdcRelationshipConfiguration : IEntityTypeConfiguration<CdcRelationship>
{
    public void Configure(EntityTypeBuilder<CdcRelationship> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Kaynak ve hedef PgConnection arasındaki CDC ilişkisi (publication/subscription/" +
            "replication slot üçlüsü); pg_publication, pg_subscription, pg_replication_slots " +
            "sistem görünümleri salt-okuma taranarak otomatik keşfedilir ya da elle tanımlanır."));

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasComment("İlişki kaydının birincil anahtarı.");
        builder.Property(r => r.SourceConnectionId).HasComment("Kaynak PgConnection.Id (publication tarafı).");
        builder.Property(r => r.TargetConnectionId).HasComment("Hedef PgConnection.Id (subscription tarafı).");
        builder.Property(r => r.PublicationName).HasMaxLength(200).IsRequired()
            .HasComment("Kaynak veritabanındaki pg_publication adı.");
        builder.Property(r => r.SubscriptionName).HasMaxLength(200).IsRequired()
            .HasComment("Hedef veritabanındaki pg_subscription adı.");
        builder.Property(r => r.SlotName).HasMaxLength(200).IsRequired()
            .HasComment("Kaynak veritabanındaki pg_replication_slots slot adı.");
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20)
            .HasComment("İlişkinin doğrulanma durumu: Inferred=otomatik keşfedildi (henüz " +
                "onaylanmadı), Confirmed=kullanıcı onayladı, Manual=elle tanımlandı, " +
                "Rejected=kullanıcı reddetti (izlemeye dahil edilmez).");
        builder.Property(r => r.ConfirmedBy).HasMaxLength(200)
            .HasComment("İlişkiyi onaylayan/reddeden kullanıcı.");
        builder.Property(r => r.ConfirmedAt).HasComment("Onaylama/reddetme zamanı.");
        builder.Property(r => r.CreatedAt).HasComment("İlişkinin keşfedildiği/oluşturulduğu zaman.");

        builder.HasIndex(r => new { r.SourceConnectionId, r.TargetConnectionId, r.SlotName }).IsUnique();

        builder.HasMany(r => r.HealthHistory)
            .WithOne(h => h.Relationship)
            .HasForeignKey(h => h.RelationshipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.ReconciliationResults)
            .WithOne(x => x.Relationship)
            .HasForeignKey(x => x.RelationshipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
