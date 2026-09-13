using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class FakePostgresInspector : IPostgresInspector
{
    public Dictionary<Guid, List<PublicationInfo>> Publications { get; } = [];
    public Dictionary<Guid, List<ReplicationSlotInfo>> Slots { get; } = [];
    public Dictionary<Guid, List<SubscriptionInfo>> Subscriptions { get; } = [];
    public Dictionary<Guid, List<SubscriptionStatInfo>> SubscriptionStats { get; } = [];
    public Dictionary<Guid, List<ReplicationStatInfo>> ReplicationStats { get; } = [];
    public Dictionary<Guid, List<PublicationTableInfo>> PublicationTables { get; } = [];
    public Dictionary<(Guid ConnectionId, string Schema, string Table), TableChecksum> TableChecksums { get; } = [];

    public Task<List<PublicationInfo>> GetPublicationsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default) =>
        Task.FromResult(Publications.GetValueOrDefault(connection.Id, []));

    public Task<List<ReplicationSlotInfo>> GetReplicationSlotsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default) =>
        Task.FromResult(Slots.GetValueOrDefault(connection.Id, []));

    public Task<List<SubscriptionInfo>> GetSubscriptionsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default) =>
        Task.FromResult(Subscriptions.GetValueOrDefault(connection.Id, []));

    public Task<List<SubscriptionStatInfo>> GetSubscriptionStatsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default) =>
        Task.FromResult(SubscriptionStats.GetValueOrDefault(connection.Id, []));

    public Task<List<ReplicationStatInfo>> GetReplicationStatsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default) =>
        Task.FromResult(ReplicationStats.GetValueOrDefault(connection.Id, []));

    public Task<List<PublicationTableInfo>> GetPublicationTablesAsync(PgConnection connection, string plaintextPassword, IReadOnlyList<string> publicationNames, CancellationToken ct = default) =>
        Task.FromResult(PublicationTables.GetValueOrDefault(connection.Id, []));

    public Task<List<string>> GetTableColumnsAsync(PgConnection connection, string plaintextPassword, string schemaName, string tableName, CancellationToken ct = default) =>
        Task.FromResult(new List<string>());

    public Task<TableChecksum> GetTableChecksumAsync(PgConnection connection, string plaintextPassword, string schemaName, string tableName, IReadOnlyList<string> columnNames, CancellationToken ct = default) =>
        Task.FromResult(TableChecksums.GetValueOrDefault((connection.Id, schemaName, tableName), new TableChecksum(0, "0")));
}
