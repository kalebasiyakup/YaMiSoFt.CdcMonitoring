using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class ConnectionHealthCheckConfiguration : IEntityTypeConfiguration<ConnectionHealthCheck>
{
    public void Configure(EntityTypeBuilder<ConnectionHealthCheck> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.PostgresVersion).HasMaxLength(200);
        builder.Property(h => h.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(h => new { h.ConnectionId, h.CheckedAt });
    }
}
