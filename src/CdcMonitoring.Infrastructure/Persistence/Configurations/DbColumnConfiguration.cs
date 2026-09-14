using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class DbColumnConfiguration : IEntityTypeConfiguration<DbColumn>
{
    public void Configure(EntityTypeBuilder<DbColumn> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Şema kataloğunun kolon seviyesi. DbTable gibi silinmez; kaynakta kalmayan " +
            "kolonlar DroppedAt ile işaretlenir."));

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasComment("Katalog kolon kaydının birincil anahtarı.");
        builder.Property(c => c.TableId).HasComment("Kolonun ait olduğu DbTable.Id.");
        builder.Property(c => c.ColumnName).HasMaxLength(200).IsRequired()
            .HasComment("Kolon adı.");
        builder.Property(c => c.OrdinalPosition).HasComment("Kolonun tablodaki sırası (1'den başlar).");
        builder.Property(c => c.DataType).HasMaxLength(200).IsRequired()
            .HasComment("Kolonun veri tipi (information_schema.columns.data_type).");
        builder.Property(c => c.IsNullable).HasComment("Kolon NULL kabul ediyor mu.");
        builder.Property(c => c.DefaultExpression).HasMaxLength(1000)
            .HasComment("Kolonun varsayılan değer ifadesi; yoksa null.");
        builder.Property(c => c.MaxLength).HasComment("Metin tipleri için karakter uzunluğu sınırı.");
        builder.Property(c => c.NumericPrecision).HasComment("Sayısal tipler için toplam basamak sayısı.");
        builder.Property(c => c.NumericScale).HasComment("Sayısal tipler için ondalık basamak sayısı.");
        builder.Property(c => c.IsPrimaryKey).HasComment("Kolon birincil anahtarın parçası mı.");
        builder.Property(c => c.Comment).HasMaxLength(2000)
            .HasComment("Kolonun PostgreSQL'deki açıklaması.");
        builder.Property(c => c.FirstSeenAt).HasComment("Kolonun katalogda ilk görüldüğü zaman.");
        builder.Property(c => c.LastSeenAt).HasComment("Kolonun en son hangi taramada görüldüğü.");
        builder.Property(c => c.DroppedAt)
            .HasComment("Kolonun kaynakta artık bulunmadığının ilk tespit edildiği zaman; null ise hâlâ mevcut.");

        builder.HasOne(c => c.Table)
            .WithMany(t => t.Columns)
            .HasForeignKey(c => c.TableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TableId, c.ColumnName }).IsUnique();

        // Katalog araması kolon adı üzerinden de yapılır (ör. "hangi tablolarda customer_id var").
        builder.HasIndex(c => c.ColumnName);
    }
}
