using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Domain.Entities;

public class CdcRelationshipHealth
{
    public Guid Id { get; set; }
    public Guid RelationshipId { get; set; }
    public CdcRelationship? Relationship { get; set; }
    public DateTimeOffset CheckedAt { get; set; }
    public bool SlotActive { get; set; }
    public string? WalStatus { get; set; }
    public long? LagBytes { get; set; }
    public SubscriptionState SubscriptionState { get; set; } = SubscriptionState.Unknown;
    public DateTimeOffset? LastSyncAt { get; set; }
}
