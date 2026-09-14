using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Infrastructure.Jobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CdcMonitoring.Infrastructure.Persistence.Configurations;

public class JobScheduleConfiguration : IEntityTypeConfiguration<JobSchedule>
{
    public void Configure(EntityTypeBuilder<JobSchedule> builder)
    {
        builder.ToTable(t => t.HasComment(
            "Çok replikalı dağıtımda hangi arka plan job'unun ne zaman çalıştığını izleyen " +
            "'claim' tablosu; atomik güncelleme ile aynı job'un iki replikada aynı anda " +
            "çalışması engellenir (bkz. IJobScheduleRepository.TryClaimAsync)."));

        builder.HasKey(j => j.JobName);
        builder.Property(j => j.JobName).HasMaxLength(100)
            .HasComment("Job'un benzersiz adı (birincil anahtar); CdcMonitoring.Infrastructure.Jobs.JobNames sabitlerinden biri.");
        builder.Property(j => j.LastRunAt).HasComment("Job'un en son başarıyla tamamlandığı zaman; null ise hiç çalışmamıştır.");

        builder.HasData(
            new JobSchedule { JobName = JobNames.ConnectionHealthCheck, LastRunAt = null },
            new JobSchedule { JobName = JobNames.CdcDiscovery, LastRunAt = null },
            new JobSchedule { JobName = JobNames.AlertEvaluation, LastRunAt = null },
            new JobSchedule { JobName = JobNames.WeeklyReconciliation, LastRunAt = null },
            new JobSchedule { JobName = JobNames.RetentionCleanup, LastRunAt = null });
    }
}
