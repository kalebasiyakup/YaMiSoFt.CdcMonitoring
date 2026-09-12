using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

public interface ICdcRelationshipHealthRepository
{
    Task AddAsync(CdcRelationshipHealth entry, CancellationToken ct = default);
    Task<CdcRelationshipHealth?> GetLatestAsync(Guid relationshipId, CancellationToken ct = default);
    Task<List<CdcRelationshipHealth>> GetSinceAsync(Guid relationshipId, DateTimeOffset since, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
