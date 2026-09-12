namespace CdcMonitoring.Application.CdcDiscovery;

public record PublicationInfo(string Name);

public record ReplicationSlotInfo(
    string SlotName,
    bool Active,
    string? WalStatus,
    long? LagBytes,
    string? Database);

public record SubscriptionInfo(
    string Name,
    bool Enabled,
    string? SlotName,
    string? ConnInfo,
    IReadOnlyList<string> Publications);

public record SubscriptionStatInfo(
    string SubscriptionName,
    bool WorkerRunning,
    DateTimeOffset? LastMessageReceiptTime);

public record ReplicationStatInfo(
    string? ApplicationName,
    string? ClientAddress,
    string State,
    string? SyncState);
