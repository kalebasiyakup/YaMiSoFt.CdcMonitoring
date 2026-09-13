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

    /// <summary>
    /// Reconciliation (FR-11) ve şema karşılaştırma ekranı için: bir tablonun kolon adı + veri
    /// tipi listesi. Reconciliation'da kaynak tarafında alınıp hem kaynak hem hedef checksum
    /// sorgusuna kolon adı olarak aynen geçirilir — hedefte (replikasyona dahil olmayan)
    /// fazladan bir kolon bulunması (ör. hedef servisin kendi eklediği bir bookkeeping alanı)
    /// checksum'ı hiçbir zaman etkilememelidir.
    /// </summary>
    Task<List<ColumnInfo>> GetTableColumnsAsync(PgConnection connection, string plaintextPassword, string schemaName, string tableName, CancellationToken ct = default);

    /// <summary>
    /// Reconciliation (FR-11) için: bir tablonun satır sayısı ve sıra bağımsız checksum'ı.
    /// columnNames, hem kaynak hem hedef için AYNI (kaynaktan alınmış) listedir — checksum'ın
    /// yalnızca gerçekten replike edilen kolonlara bağlı olmasını, hedefteki fazladan kolonlardan
    /// etkilenmemesini garanti eder.
    /// </summary>
    Task<TableChecksum> GetTableChecksumAsync(PgConnection connection, string plaintextPassword, string schemaName, string tableName, IReadOnlyList<string> columnNames, CancellationToken ct = default);
}
