using CdcMonitoring.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class JobScheduleRepository(CdcMonitoringDbContext db) : IJobScheduleRepository
{
    public async Task<bool> TryClaimAsync(string jobName, TimeSpan interval, DateTimeOffset now, CancellationToken ct = default)
    {
        var cutoff = now - interval;

        // Tek bir koşullu UPDATE: yalnızca hiç çalışmamış veya aralık dolmuş job'lar
        // güncellenir. Aynı anda birden fazla replika bunu çalıştırırsa, veritabanı
        // bu satır için kilitlemeyi seri hale getirir; yalnızca biri satırı etkiler (NFR-05).
        var affected = await db.JobSchedules
            .Where(j => j.JobName == jobName && (j.LastRunAt == null || j.LastRunAt <= cutoff))
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LastRunAt, now), ct);

        return affected > 0;
    }

    public async Task ReleaseClaimAsync(string jobName, CancellationToken ct = default)
    {
        await db.JobSchedules
            .Where(j => j.JobName == jobName)
            .ExecuteUpdateAsync(s => s.SetProperty(j => j.LastRunAt, (DateTimeOffset?)null), ct);
    }
}
