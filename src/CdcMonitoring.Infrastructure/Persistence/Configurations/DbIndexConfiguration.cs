using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class DbIndexConfiguration : IEntityTypeConfiguration<DbIndex>
{
    public void Configure(EntityTypeBuilder<DbIndex> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Şema kataloğunun indeks seviyesi. Definition alanı pg_get_indexdef çıktısının " +
            "aynen saklanmış halidir; uygulama bu metni hiçbir zaman çalıştırmaz (FR-14)."));

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasComment("Katalog indeks kaydının birincil anahtarı.");
        builder.Property(i => i.TableId).HasComment("İndeksin ait olduğu DbTable.Id.");
        builder.Property(i => i.IndexName).HasMaxLength(200).IsRequired()
            .HasComment("İndeks adı.");
        builder.Property(i => i.IsUnique).HasComment("İndeks tekil (unique) mi.");
        builder.Property(i => i.IsPrimary).HasComment("İndeks birincil anahtarın indeksi mi.");
        builder.Property(i => i.ColumnsCsv).HasMaxLength(2000).IsRequired()
            .HasComment("İndekse dahil kolon adları, sırasıyla virgülle ayrılmış.");
        builder.Property(i => i.Definition).HasMaxLength(4000).IsRequired()
            .HasComment("pg_get_indexdef çıktısı; yalnızca görüntüleme amaçlı saklanır.");
        builder.Property(i => i.SizeBytes).HasComment("İndeksin disk boyutu (byte).");
        builder.Property(i => i.FirstSeenAt).HasComment("İndeksin katalogda ilk görüldüğü zaman.");
        builder.Property(i => i.LastSeenAt).HasComment("İndeksin en son hangi taramada görüldüğü.");
        builder.Property(i => i.DroppedAt)
            .HasComment("İndeksin kaynakta artık bulunmadığının ilk tespit edildiği zaman; null ise hâlâ mevcut.");

        builder.HasOne(i => i.Table)
            .WithMany(t => t.Indexes)
            .HasForeignKey(i => i.TableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(i => new { i.TableId, i.IndexName }).IsUnique();
    }
}
