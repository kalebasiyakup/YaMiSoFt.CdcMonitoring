using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class DbTableConfiguration : IEntityTypeConfiguration<DbTable>
{
    public void Configure(EntityTypeBuilder<DbTable> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Şema kataloğunun tablo/görünüm seviyesi. Her tarama bu satırları günceller; " +
            "kaynakta artık bulunmayan nesneler silinmez, DroppedAt ile işaretlenir."));

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).HasComment("Katalog tablo kaydının birincil anahtarı.");
        builder.Property(t => t.ConnectionId).HasComment("Tablonun bulunduğu PgConnection.Id.");
        builder.Property(t => t.SchemaName).HasMaxLength(200).IsRequired()
            .HasComment("PostgreSQL şema adı (ör. public).");
        builder.Property(t => t.TableName).HasMaxLength(200).IsRequired()
            .HasComment("Tablo/görünüm adı.");
        builder.Property(t => t.Kind).HasConversion<string>().HasMaxLength(30).IsRequired()
            .HasComment("Nesne türü: Table, PartitionedTable, View, MaterializedView, ForeignTable.");
        builder.Property(t => t.EstimatedRowCount)
            .HasComment("pg_class.reltuples — planlayıcının tahmini satır sayısı, gerçek COUNT(*) değildir.");
        builder.Property(t => t.TotalSizeBytes)
            .HasComment("pg_total_relation_size — indeks ve TOAST dahil toplam boyut (byte).");
        builder.Property(t => t.HasPrimaryKey).HasComment("Tablonun birincil anahtarı var mı.");
        builder.Property(t => t.IsPublished)
            .HasComment("Tablo bu bağlantıdaki herhangi bir publication'a dahil mi (CDC kapsamında mı).");
        builder.Property(t => t.Comment).HasMaxLength(2000)
            .HasComment("Tablonun PostgreSQL'deki açıklaması.");
        builder.Property(t => t.FirstSeenAt).HasComment("Tablonun katalogda ilk görüldüğü zaman.");
        builder.Property(t => t.LastSeenAt).HasComment("Tablonun en son hangi taramada görüldüğü.");
        builder.Property(t => t.DroppedAt)
            .HasComment("Tablonun kaynakta artık bulunmadığının ilk tespit edildiği zaman; null ise hâlâ mevcut.");

        builder.HasOne(t => t.Connection)
            .WithMany()
            .HasForeignKey(t => t.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.ConnectionId, t.SchemaName, t.TableName }).IsUnique();
    }
}
