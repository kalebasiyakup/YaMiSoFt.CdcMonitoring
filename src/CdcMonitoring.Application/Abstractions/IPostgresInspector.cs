using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

/// <summary>
/// pg_replication_slots, pg_publication, pg_subscription, pg_stat_subscription,
/// pg_stat_replication görünümlerini salt-okuma ile sorgular (FR-04). Bu arayüzün
/// hiçbir implementasyonu izlenen PostgreSQL örneklerinde DDL/DML çalıştırmamalıdır (FR-14).
/// </summary>
public interface IPostgresInspector
{
    Task<List<PublicationInfo>> GetPublicationsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default);
    Task<List<ReplicationSlotInfo>> GetReplicationSlotsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default);
    Task<List<SubscriptionInfo>> GetSubscriptionsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default);
    Task<List<SubscriptionStatInfo>> GetSubscriptionStatsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default);
    Task<List<ReplicationStatInfo>> GetReplicationStatsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default);

    /// <summary>Reconciliation (FR-11) için: publication'ın kapsadığı tabloları listeler.</summary>
    Task<List<PublicationTableInfo>> GetPublicationTablesAsync(PgConnection connection, string plaintextPassword, IReadOnlyList<string> publicationNames, CancellationToken ct = default);

    /// <summary>Reconciliation (FR-11) için: bir tablonun satır sayısı ve sıra bağımsız checksum'ı.</summary>
    Task<TableChecksum> GetTableChecksumAsync(PgConnection connection, string plaintextPassword, string schemaName, string tableName, CancellationToken ct = default);
}
