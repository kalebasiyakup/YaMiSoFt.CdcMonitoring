using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence.Repositories;

public class ReconciliationResultRepository(CdcMonitoringDbContext db) : IReconciliationResultRepository
{
    public Task<List<ReconciliationResult>> GetRecentAsync(int take, CancellationToken ct = default) =>
        db.ReconciliationResults.OrderByDescending(r => r.RunAt).Take(take).ToListAsync(ct);

    public async Task AddAsync(ReconciliationResult result, CancellationToken ct = default) =>
        await db.ReconciliationResults.AddAsync(result, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
