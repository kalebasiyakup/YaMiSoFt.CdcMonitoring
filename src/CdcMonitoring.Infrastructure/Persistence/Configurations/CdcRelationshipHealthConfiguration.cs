using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class CdcRelationshipHealthConfiguration : IEntityTypeConfiguration<CdcRelationshipHealth>
{
    public void Configure(EntityTypeBuilder<CdcRelationshipHealth> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.WalStatus).HasMaxLength(50);
        builder.Property(h => h.SubscriptionState).HasConversion<string>().HasMaxLength(20);
        builder.HasIndex(h => new { h.RelationshipId, h.CheckedAt });
    }
}
