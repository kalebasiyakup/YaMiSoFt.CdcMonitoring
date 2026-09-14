using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class SchemaScanConfiguration : IEntityTypeConfiguration<SchemaScan>
{
    public void Configure(EntityTypeBuilder<SchemaScan> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Bir bağlantı için çalıştırılan şema katalog taramasının sonucu (ne zaman, ne " +
            "kadar sürdü, başarılı mı). Katalog verisinin kendisi DbTables/DbColumns/" +
            "DbIndexes/DbConstraints tablolarında güncel durum olarak tutulur."));

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasComment("Tarama kaydının birincil anahtarı.");
        builder.Property(s => s.ConnectionId).HasComment("Taranan PgConnection.Id.");
        builder.Property(s => s.StartedAt).HasComment("Taramanın başladığı zaman.");
        builder.Property(s => s.CompletedAt).HasComment("Taramanın bittiği zaman (hata halinde de doldurulur).");
        builder.Property(s => s.Success).HasComment("Tarama hatasız tamamlandıysa true.");
        builder.Property(s => s.ErrorMessage).HasMaxLength(2000)
            .HasComment("Tarama başarısızsa hata mesajı.");
        builder.Property(s => s.TableCount).HasComment("Taramada bulunan tablo/görünüm sayısı.");
        builder.Property(s => s.ColumnCount).HasComment("Taramada bulunan toplam kolon sayısı.");
        builder.Property(s => s.DurationMs).HasComment("Taramanın süresi (milisaniye).");

        builder.HasOne(s => s.Connection)
            .WithMany()
            .HasForeignKey(s => s.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => new { s.ConnectionId, s.StartedAt });

        // Retention temizliği (salt StartedAt < cutoff) tüm tabloyu taramadan çalışsın.
        builder.HasIndex(s => s.StartedAt);
    }
}
