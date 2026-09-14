using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class ReconciliationResultConfiguration : IEntityTypeConfiguration<ReconciliationResult>
{
    public void Configure(EntityTypeBuilder<ReconciliationResult> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Onaylanmış CdcRelationship'ler için Veri Tutarlılık Kontrolü sonuçları: kaynak-" +
            "hedef satır sayısı ve checksum karşılaştırması (checksum yalnızca publication'da " +
            "yayınlanan kolonları kapsar). ReconciliationRetentionDays süresine göre eski " +
            "kayıtlar otomatik silinir."));

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasComment("Kontrol sonucunun birincil anahtarı.");
        builder.Property(r => r.RelationshipId).HasComment("Bağlı olduğu CdcRelationship.Id.");
        builder.Property(r => r.RunAt).HasComment("Kontrolün çalıştırıldığı zaman.");
        builder.Property(r => r.SourceRowCount).HasComment("Kaynak tablodaki satır sayısı.");
        builder.Property(r => r.TargetRowCount).HasComment("Hedef tablodaki satır sayısı.");
        builder.Property(r => r.SourceChecksum).HasMaxLength(200)
            .HasComment("Kaynak tablonun yayınlanan kolonlar üzerinden hesaplanan checksum'ı.");
        builder.Property(r => r.TargetChecksum).HasMaxLength(200)
            .HasComment("Hedef tablonun aynı kolonlar üzerinden hesaplanan checksum'ı.");
        builder.Property(r => r.IsMatch).HasComment("Satır sayısı ve checksum'ların eşleşip eşleşmediği.");
        builder.Property(r => r.Details).HasMaxLength(4000)
            .HasComment("Tutarsızlık varsa ayrıntı metni (ör. fark eden satır/kolon bilgisi).");
        builder.HasIndex(r => new { r.RelationshipId, r.RunAt });

        // Retention temizliğinin (ilişkiden bağımsız, salt RunAt < cutoff) tüm tabloyu
        // taramadan çalışabilmesi için ayrı bir indeks.
        builder.HasIndex(r => r.RunAt);
    }
}
