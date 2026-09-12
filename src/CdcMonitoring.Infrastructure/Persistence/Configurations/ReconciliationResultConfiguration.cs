using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class ReconciliationResultConfiguration : IEntityTypeConfiguration<ReconciliationResult>
{
    public void Configure(EntityTypeBuilder<ReconciliationResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.SourceChecksum).HasMaxLength(200);
        builder.Property(r => r.TargetChecksum).HasMaxLength(200);
        builder.Property(r => r.Details).HasMaxLength(4000);
        builder.HasIndex(r => new { r.RelationshipId, r.RunAt });
    }
}
