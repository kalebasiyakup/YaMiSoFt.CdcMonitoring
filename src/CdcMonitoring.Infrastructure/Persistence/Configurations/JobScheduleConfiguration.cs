using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class JobScheduleConfiguration : IEntityTypeConfiguration<JobSchedule>
{
    public void Configure(EntityTypeBuilder<JobSchedule> builder)
    {
        builder.HasKey(j => j.JobName);
        builder.Property(j => j.JobName).HasMaxLength(100);

        builder.HasData(
            new JobSchedule { JobName = JobNames.ConnectionHealthCheck, LastRunAt = null },
            new JobSchedule { JobName = JobNames.CdcDiscovery, LastRunAt = null },
            new JobSchedule { JobName = JobNames.AlertEvaluation, LastRunAt = null },
            new JobSchedule { JobName = JobNames.WeeklyReconciliation, LastRunAt = null });
    }
}
