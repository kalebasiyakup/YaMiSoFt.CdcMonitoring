using System.Text.Json;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.CdcDiscovery;

public class CdcRelationshipManagementService(
    ICdcRelationshipRepository relationships,
    ICdcRelationshipHealthRepository relationshipHealth,
    IPgConnectionRepository connections,
    IAuditLogRepository auditLog,
    ICurrentUserAccessor currentUser,
    IClock clock)
{
    public async Task<List<CdcRelationshipSummary>> ListAsync(CancellationToken ct = default)
    {
        var all = await relationships.GetAllAsync(ct);
        var result = new List<CdcRelationshipSummary>(all.Count);

        foreach (var r in all)
        {
            var latest = await relationshipHealth.GetLatestAsync(r.Id, ct);
            result.Add(ToSummary(r, latest));
        }

        return result.OrderBy(r => r.SourceConnectionName).ThenBy(r => r.TargetConnectionName).ToList();
    }

    public async Task ConfirmAsync(Guid relationshipId, CancellationToken ct = default)
    {
        var relationship = await relationships.GetByIdAsync(relationshipId, ct)
            ?? throw new KeyNotFoundException($"İlişki bulunamadı: {relationshipId}");

        var actor = currentUser.GetCurrentUserNameOrDefault();
        var now = clock.UtcNow;
        var before = relationship.Status;

        relationship.Status = CdcRelationshipStatus.Confirmed;
        relationship.ConfirmedBy = actor;
        relationship.ConfirmedAt = now;
        await relationships.SaveChangesAsync(ct);

        await WriteAuditAsync(relationship.Id, AuditAction.Confirmed, actor, now, before, relationship.Status, ct);
    }

    public async Task RejectAsync(Guid relationshipId, CancellationToken ct = default)
    {
        var relationship = await relationships.GetByIdAsync(relationshipId, ct)
            ?? throw new KeyNotFoundException($"İlişki bulunamadı: {relationshipId}");

        var actor = currentUser.GetCurrentUserNameOrDefault();
        var now = clock.UtcNow;
        var before = relationship.Status;

        relationship.Status = CdcRelationshipStatus.Rejected;
        relationship.ConfirmedBy = actor;
        relationship.ConfirmedAt = now;
        await relationships.SaveChangesAsync(ct);

        await WriteAuditAsync(relationship.Id, AuditAction.Rejected, actor, now, before, relationship.Status, ct);
    }

    public async Task<CdcRelationshipSummary> CreateManualAsync(ManualRelationshipRequest request, CancellationToken ct = default)
    {
        var source = await connections.GetByIdAsync(request.SourceConnectionId, ct)
            ?? throw new KeyNotFoundException($"Kaynak bağlantı bulunamadı: {request.SourceConnectionId}");
        var target = await connections.GetByIdAsync(request.TargetConnectionId, ct)
            ?? throw new KeyNotFoundException($"Hedef bağlantı bulunamadı: {request.TargetConnectionId}");

        if (source.Id == target.Id)
            throw new InvalidOperationException("Kaynak ve hedef bağlantı aynı olamaz.");

        var existing = await relationships.FindAsync(source.Id, target.Id, request.SlotName, ct);
        if (existing is not null)
            throw new InvalidOperationException("Bu kaynak/hedef/slot kombinasyonu için zaten bir ilişki kayıtlı.");

        var actor = currentUser.GetCurrentUserNameOrDefault();
        var now = clock.UtcNow;

        var relationship = new CdcRelationship
        {
            Id = Guid.NewGuid(),
            SourceConnectionId = source.Id,
            TargetConnectionId = target.Id,
            PublicationName = request.PublicationName,
            SubscriptionName = request.SubscriptionName,
            SlotName = request.SlotName,
            Status = CdcRelationshipStatus.Manual,
            ConfirmedBy = actor,
            ConfirmedAt = now,
            CreatedAt = now
        };

        await relationships.AddAsync(relationship, ct);
        await relationships.SaveChangesAsync(ct);

        await WriteAuditAsync(relationship.Id, AuditAction.Created, actor, now, oldValue: null, newValue: new
        {
            relationship.SourceConnectionId,
            relationship.TargetConnectionId,
            relationship.PublicationName,
            relationship.SubscriptionName,
            relationship.SlotName,
            relationship.Status
        }, ct);

        return ToSummary(relationship, latestHealth: null, sourceName: source.Name, targetName: target.Name);
    }

    private async Task WriteAuditAsync(Guid entityId, AuditAction action, string actor, DateTimeOffset at, object? oldValue, object? newValue, CancellationToken ct)
    {
        await auditLog.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(CdcRelationship),
            EntityId = entityId,
            Action = action,
            ChangedBy = actor,
            ChangedAt = at,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue)
        }, ct);
    }

    private static CdcRelationshipSummary ToSummary(CdcRelationship r, CdcRelationshipHealth? latestHealth, string? sourceName = null, string? targetName = null) => new(
        r.Id,
        r.SourceConnectionId,
        sourceName ?? r.SourceConnection?.Name ?? "?",
        r.TargetConnectionId,
        targetName ?? r.TargetConnection?.Name ?? "?",
        r.PublicationName,
        r.SubscriptionName,
        r.SlotName,
        r.Status,
        r.ConfirmedBy,
        r.ConfirmedAt,
        r.CreatedAt,
        latestHealth is null ? null : new CdcRelationshipHealthSummary(
            latestHealth.CheckedAt, latestHealth.SlotActive, latestHealth.WalStatus,
            latestHealth.LagBytes, latestHealth.SubscriptionState, latestHealth.LastSyncAt));
}
