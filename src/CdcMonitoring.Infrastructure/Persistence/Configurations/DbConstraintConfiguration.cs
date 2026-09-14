using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class DbConstraintConfiguration : IEntityTypeConfiguration<DbConstraint>
{
    public void Configure(EntityTypeBuilder<DbConstraint> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Şema kataloğunun kısıt seviyesi (PK/FK/unique/check). Definition alanı " +
            "pg_get_constraintdef çıktısının aynen saklanmış halidir; uygulama bu metni " +
            "hiçbir zaman çalıştırmaz (FR-14)."));

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasComment("Katalog kısıt kaydının birincil anahtarı.");
        builder.Property(c => c.TableId).HasComment("Kısıtın ait olduğu DbTable.Id.");
        builder.Property(c => c.ConstraintName).HasMaxLength(200).IsRequired()
            .HasComment("Kısıt adı.");
        builder.Property(c => c.Kind).HasConversion<string>().HasMaxLength(20).IsRequired()
            .HasComment("Kısıt türü: PrimaryKey, ForeignKey, Unique, Check, Exclusion, Other.");
        builder.Property(c => c.ColumnsCsv).HasMaxLength(2000).IsRequired()
            .HasComment("Kısıta dahil kolon adları, sırasıyla virgülle ayrılmış.");
        builder.Property(c => c.ReferencedSchema).HasMaxLength(200)
            .HasComment("Yalnızca ForeignKey kısıtlarında dolu: hedef tablonun şeması.");
        builder.Property(c => c.ReferencedTable).HasMaxLength(200)
            .HasComment("Yalnızca ForeignKey kısıtlarında dolu: hedef tablonun adı.");
        builder.Property(c => c.ReferencedColumnsCsv).HasMaxLength(2000)
            .HasComment("Yalnızca ForeignKey kısıtlarında dolu: hedef kolon adları.");
        builder.Property(c => c.Definition).HasMaxLength(4000).IsRequired()
            .HasComment("pg_get_constraintdef çıktısı; yalnızca görüntüleme amaçlı saklanır.");
        builder.Property(c => c.FirstSeenAt).HasComment("Kısıtın katalogda ilk görüldüğü zaman.");
        builder.Property(c => c.LastSeenAt).HasComment("Kısıtın en son hangi taramada görüldüğü.");
        builder.Property(c => c.DroppedAt)
            .HasComment("Kısıtın kaynakta artık bulunmadığının ilk tespit edildiği zaman; null ise hâlâ mevcut.");

        builder.HasOne(c => c.Table)
            .WithMany(t => t.Constraints)
            .HasForeignKey(c => c.TableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.TableId, c.ConstraintName }).IsUnique();
    }
}
