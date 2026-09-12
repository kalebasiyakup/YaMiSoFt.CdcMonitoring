using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class InMemoryCdcRelationshipHealthRepository : ICdcRelationshipHealthRepository
{
    public List<CdcRelationshipHealth> Entries { get; } = [];

    public Task AddAsync(CdcRelationshipHealth entry, CancellationToken ct = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task<CdcRelationshipHealth?> GetLatestAsync(Guid relationshipId, CancellationToken ct = default) =>
        Task.FromResult(Entries.Where(e => e.RelationshipId == relationshipId)
            .OrderByDescending(e => e.CheckedAt)
            .FirstOrDefault());

    public Task<List<CdcRelationshipHealth>> GetSinceAsync(Guid relationshipId, DateTimeOffset since, CancellationToken ct = default) =>
        Task.FromResult(Entries.Where(e => e.RelationshipId == relationshipId && e.CheckedAt >= since)
            .OrderByDescending(e => e.CheckedAt)
            .ToList());

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;
}
