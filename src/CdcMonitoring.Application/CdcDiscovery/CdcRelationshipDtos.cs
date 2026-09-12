using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.CdcDiscovery;

public record CdcRelationshipHealthSummary(
    DateTimeOffset CheckedAt,
    bool SlotActive,
    string? WalStatus,
    long? LagBytes,
    SubscriptionState SubscriptionState,
    DateTimeOffset? LastSyncAt);

public record CdcRelationshipSummary(
    Guid Id,
    Guid SourceConnectionId,
    string SourceConnectionName,
    Guid TargetConnectionId,
    string TargetConnectionName,
    string PublicationName,
    string SubscriptionName,
    string SlotName,
    CdcRelationshipStatus Status,
    string? ConfirmedBy,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset CreatedAt,
    CdcRelationshipHealthSummary? LatestHealth);

public record ManualRelationshipRequest(
    Guid SourceConnectionId,
    Guid TargetConnectionId,
    string PublicationName,
    string SubscriptionName,
    string SlotName);
