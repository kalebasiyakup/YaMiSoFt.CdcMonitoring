using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class SchemaChangeEventConfiguration : IEntityTypeConfiguration<SchemaChangeEvent>
{
    public void Configure(EntityTypeBuilder<SchemaChangeEvent> builder)
    {
        builder.ToTable(t => t.HasComment(
            "İki katalog taraması arasında tespit edilen şema değişiklikleri. Katalog " +
            "tabloları yalnızca güncel durumu tuttuğu için değişim geçmişi burada birikir; " +
            "saklama süresi SystemSettings.SchemaChangeRetentionDays ile sınırlanır."));

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasComment("Değişiklik kaydının birincil anahtarı.");
        builder.Property(e => e.ConnectionId).HasComment("Değişikliğin görüldüğü PgConnection.Id.");
        builder.Property(e => e.SchemaName).HasMaxLength(200).IsRequired()
            .HasComment("Değişen nesnenin şeması.");
        builder.Property(e => e.TableName).HasMaxLength(200).IsRequired()
            .HasComment("Değişen nesnenin tablosu.");
        builder.Property(e => e.ObjectName).HasMaxLength(200)
            .HasComment("Değişen alt nesnenin adı (kolon/indeks/kısıt); tablo seviyesi değişimlerde null.");
        builder.Property(e => e.ChangeType).HasConversion<string>().HasMaxLength(40).IsRequired()
            .HasComment("Değişiklik türü (ör. ColumnAdded, ColumnTypeChanged, TableDropped).");
        builder.Property(e => e.OldValue).HasMaxLength(4000)
            .HasComment("Değişiklik öncesi değer (varsa).");
        builder.Property(e => e.NewValue).HasMaxLength(4000)
            .HasComment("Değişiklik sonrası değer (varsa).");
        builder.Property(e => e.DetectedAt).HasComment("Değişikliğin tespit edildiği tarama zamanı.");

        builder.HasOne(e => e.Connection)
            .WithMany()
            .HasForeignKey(e => e.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.ConnectionId, e.DetectedAt });

        // Retention temizliği (salt DetectedAt < cutoff) tüm tabloyu taramadan çalışsın.
        builder.HasIndex(e => e.DetectedAt);
    }
}
