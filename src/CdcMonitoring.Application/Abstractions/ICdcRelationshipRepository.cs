using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

public interface ICdcRelationshipRepository
{
    Task<List<CdcRelationship>> GetAllAsync(CancellationToken ct = default);
    Task<CdcRelationship?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CdcRelationship?> FindAsync(Guid sourceConnectionId, Guid targetConnectionId, string slotName, CancellationToken ct = default);
    Task AddAsync(CdcRelationship relationship, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
