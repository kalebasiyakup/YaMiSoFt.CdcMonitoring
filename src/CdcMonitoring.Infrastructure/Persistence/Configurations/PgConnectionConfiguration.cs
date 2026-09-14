using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class PgConnectionConfiguration : IEntityTypeConfiguration<PgConnection>
{
    public void Configure(EntityTypeBuilder<PgConnection> builder)
    {
        builder.ToTable(t => t.HasComment(
            "İzlenen PostgreSQL sunucularının kayıt defteri. Uygulama buradaki bağlantılara " +
            "karşı salt-okuma çalışır; publication/subscription/replication slot asla " +
            "oluşturmaz, değiştirmez veya silmez."));

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasComment("Bağlantı kaydının birincil anahtarı.");
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired()
            .HasComment("Bağlantıya verilen görünen ad.");
        builder.Property(c => c.Host).HasMaxLength(255).IsRequired()
            .HasComment("PostgreSQL sunucusunun host adresi.");
        builder.Property(c => c.Port)
            .HasComment("PostgreSQL sunucusunun port numarası (varsayılan 5432).");
        builder.Property(c => c.DatabaseName).HasMaxLength(200).IsRequired()
            .HasComment("Bağlanılacak veritabanı adı.");
        builder.Property(c => c.Username).HasMaxLength(200).IsRequired()
            .HasComment("Bağlantı için kullanılan PostgreSQL kullanıcı adı.");
        builder.Property(c => c.EncryptedPassword).IsRequired()
            .HasComment("ASP.NET Core Data Protection ile şifrelenmiş parola; hiçbir ekranda " +
                "çözülmüş haliyle geri gösterilmez.");
        builder.Property(c => c.SslMode).HasConversion<string>().HasMaxLength(20).IsRequired()
            .HasComment("Npgsql SSL modu: Disable/Allow/Prefer/Require/VerifyCA/VerifyFull.");
        builder.Property(c => c.TrustServerCertificate)
            .HasComment("Yalnızca SslMode=Require ile anlamlı; kendi-imzalı sertifikalı iç ağ " +
                "PostgreSQL örnekleri için sunucu sertifikası doğrulamasını atlar " +
                "(VerifyCA/VerifyFull bu bayraktan etkilenmez, her koşulda doğrulama yapar).");
        builder.Property(c => c.EnvironmentTag).HasMaxLength(100).IsRequired()
            .HasComment("Ortam etiketi (ör. Prod/Test/Dev); UI'da ve topoloji grafiğinde gösterilir.");
        builder.Property(c => c.Description).HasMaxLength(1000)
            .HasComment("Bağlantı hakkında serbest metin açıklama.");
        builder.Property(c => c.IsActive)
            .HasComment("false ise bağlantı health check/discovery döngülerine dahil edilmez.");
        builder.Property(c => c.CreatedAt).HasComment("Kaydın oluşturulma zamanı.");
        builder.Property(c => c.CreatedBy).HasMaxLength(200).IsRequired()
            .HasComment("Kaydı oluşturan kullanıcı.");
        builder.Property(c => c.UpdatedAt).HasComment("Kaydın son güncellenme zamanı (yoksa hiç güncellenmemiştir).");
        builder.Property(c => c.UpdatedBy).HasMaxLength(200)
            .HasComment("Kaydı son güncelleyen kullanıcı.");

        builder.HasIndex(c => new { c.Host, c.Port, c.DatabaseName }).IsUnique();

        builder.HasMany(c => c.HealthChecks)
            .WithOne(h => h.Connection)
            .HasForeignKey(h => h.ConnectionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.SourceRelationships)
            .WithOne(r => r.SourceConnection)
            .HasForeignKey(r => r.SourceConnectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.TargetRelationships)
            .WithOne(r => r.TargetConnection)
            .HasForeignKey(r => r.TargetConnectionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
