using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class CdcRelationshipConfiguration : IEntityTypeConfiguration<CdcRelationship>
{
    public void Configure(EntityTypeBuilder<CdcRelationship> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.PublicationName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.SubscriptionName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.SlotName).HasMaxLength(200).IsRequired();
        builder.Property(r => r.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(r => r.ConfirmedBy).HasMaxLength(200);

        builder.HasIndex(r => new { r.SourceConnectionId, r.TargetConnectionId, r.SlotName }).IsUnique();

        builder.HasMany(r => r.HealthHistory)
            .WithOne(h => h.Relationship)
            .HasForeignKey(h => h.RelationshipId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.ReconciliationResults)
            .WithOne(x => x.Relationship)
            .HasForeignKey(x => x.RelationshipId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
