using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Hangi entity'de kimin ne değiştirdiğinin denetim kaydı; eski/yeni değerler JSON " +
            "olarak tutulur."));

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasComment("Denetim kaydının birincil anahtarı.");
        builder.Property(a => a.EntityType).HasMaxLength(100).IsRequired()
            .HasComment("Değişen entity'nin türü (ör. PgConnection, CdcRelationship).");
        builder.Property(a => a.EntityId).HasComment("Değişen entity kaydının Id'si.");
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(20)
            .HasComment("Yapılan işlem: Created, Updated, Deleted, Confirmed, Rejected.");
        builder.Property(a => a.ChangedBy).HasMaxLength(200).IsRequired()
            .HasComment("Değişikliği yapan kullanıcı (kimlik doğrulama entegre edilene kadar \"system\").");
        builder.Property(a => a.ChangedAt).HasComment("Değişikliğin yapıldığı zaman.");
        builder.Property(a => a.OldValue).HasComment("Değişiklik öncesi değer (JSON), varsa.");
        builder.Property(a => a.NewValue).HasComment("Değişiklik sonrası değer (JSON), varsa.");
        builder.HasIndex(a => new { a.EntityType, a.EntityId, a.ChangedAt });
    }
}
