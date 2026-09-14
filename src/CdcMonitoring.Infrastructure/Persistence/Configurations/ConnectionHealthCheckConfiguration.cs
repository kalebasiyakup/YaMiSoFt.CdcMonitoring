using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class ConnectionHealthCheckConfiguration : IEntityTypeConfiguration<ConnectionHealthCheck>
{
    public void Configure(EntityTypeBuilder<ConnectionHealthCheck> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Bir PgConnection'ın periyodik erişilebilirlik kontrolü (up/down, gecikme, " +
            "Postgres versiyonu). Ardışık başarısızlık sayısı HealthCheckFailed alarmını besler."));

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).HasComment("Kontrol kaydının birincil anahtarı.");
        builder.Property(h => h.ConnectionId).HasComment("Kontrol edilen PgConnection.Id.");
        builder.Property(h => h.CheckedAt).HasComment("Kontrolün yapıldığı zaman.");
        builder.Property(h => h.IsUp).HasComment("Bağlantının o an kurulabilip kurulamadığı.");
        builder.Property(h => h.LatencyMs).HasComment("Bağlantı kurma gecikmesi, milisaniye cinsinden (başarısızsa null).");
        builder.Property(h => h.PostgresVersion).HasMaxLength(200)
            .HasComment("Sunucudan okunan PostgreSQL sürüm bilgisi (SELECT version()).");
        builder.Property(h => h.ErrorMessage).HasMaxLength(2000)
            .HasComment("Bağlantı başarısızsa alınan hata mesajı.");
        builder.HasIndex(h => new { h.ConnectionId, h.CheckedAt });
    }
}
