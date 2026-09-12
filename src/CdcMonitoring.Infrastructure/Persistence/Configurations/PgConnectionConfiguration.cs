using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class PgConnectionConfiguration : IEntityTypeConfiguration<PgConnection>
{
    public void Configure(EntityTypeBuilder<PgConnection> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Host).HasMaxLength(255).IsRequired();
        builder.Property(c => c.DatabaseName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Username).HasMaxLength(200).IsRequired();
        builder.Property(c => c.EncryptedPassword).IsRequired();
        builder.Property(c => c.SslMode).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(c => c.EnvironmentTag).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(1000);
        builder.Property(c => c.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(c => c.UpdatedBy).HasMaxLength(200);

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
