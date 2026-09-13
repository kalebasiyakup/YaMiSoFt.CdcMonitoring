using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Domain.Entities;
using Npgsql;

namespace CdcMonitoring.Infrastructure.Postgres;

public class NpgsqlPostgresInspector : IPostgresInspector
{
    // FR-14: bu sınıfın izlenen PostgreSQL örneklerine karşı ürettiği tüm sorgular
    // burada sabit olarak tanımlıdır; CdcMonitoring.UnitTests/Postgres altındaki
    // guard test hepsinin SELECT ile başladığını doğrular.
    internal const string PublicationsQuery = "SELECT pubname FROM pg_catalog.pg_publication;";

    internal const string ReplicationSlotsQuery = """
        SELECT
            slot_name,
            active,
            wal_status,
            CASE WHEN confirmed_flush_lsn IS NOT NULL
                 THEN pg_wal_lsn_diff(pg_current_wal_lsn(), confirmed_flush_lsn)
                 ELSE NULL END AS lag_bytes,
            database
        FROM pg_catalog.pg_replication_slots
        WHERE slot_type = 'logical';
        """;

    internal const string SubscriptionsQuery = """
        SELECT subname, subenabled, subslotname, subconninfo, subpublications
        FROM pg_catalog.pg_subscription
        WHERE subdbid = (SELECT oid FROM pg_catalog.pg_database WHERE datname = current_database());
        """;

    internal const string SubscriptionStatsQuery = """
        SELECT subname, (pid IS NOT NULL) AS worker_running, last_msg_receipt_time
        FROM pg_catalog.pg_stat_subscription
        WHERE relid IS NULL;
        """;

    internal const string ReplicationStatsQuery = """
        SELECT application_name, client_addr::text AS client_addr, state, sync_state
        FROM pg_catalog.pg_stat_replication;
        """;

    internal const string PublicationTablesQuery = """
        SELECT DISTINCT schemaname, tablename
        FROM pg_catalog.pg_publication_tables
        WHERE pubname = ANY(@pubnames);
        """;

    internal const string TableColumnsQuery = """
        SELECT column_name
        FROM information_schema.columns
        WHERE table_schema = @schema AND table_name = @table
        ORDER BY ordinal_position;
        """;

    /// <summary>
    /// Reconciliation için sıra bağımsız (order-independent) checksum: her satırın
    /// metin gösteriminin md5'inin ilk 64 bit'i toplanır. Salt-okuma, DDL/DML içermez (FR-14).
    /// Kolon listesi (quotedColumns) her iki tarafta da AYNEN aynı (kaynaktan alınmış) olmalıdır —
    /// aksi halde hedefteki fazladan bir kolon (ör. hedef servisin kendi eklediği bir bookkeeping
    /// alanı) her satırın metin gösterimini değiştirip checksum'ı her zaman uyuşmaz hale getirir.
    /// </summary>
    internal static string BuildTableChecksumQuery(string quotedSchema, string quotedTable, IReadOnlyList<string> quotedColumns) => $"""
        SELECT
            count(*)::bigint AS row_count,
            COALESCE(sum(('x' || substr(md5(t::text), 1, 16))::bit(64)::bigint), 0)::text AS checksum
        FROM (SELECT {string.Join(", ", quotedColumns)} FROM {quotedSchema}.{quotedTable}) t;
        """;

    public async Task<List<PublicationInfo>> GetPublicationsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);
        await using var cmd = new NpgsqlCommand(PublicationsQuery, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<PublicationInfo>();
        while (await reader.ReadAsync(ct))
            result.Add(new PublicationInfo(reader.GetString(0)));
        return result;
    }

    public async Task<List<ReplicationSlotInfo>> GetReplicationSlotsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);
        await using var cmd = new NpgsqlCommand(ReplicationSlotsQuery, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<ReplicationSlotInfo>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ReplicationSlotInfo(
                SlotName: reader.GetString(0),
                Active: reader.GetBoolean(1),
                WalStatus: reader.IsDBNull(2) ? null : reader.GetString(2),
                LagBytes: reader.IsDBNull(3) ? null : reader.GetInt64(3),
                Database: reader.IsDBNull(4) ? null : reader.GetString(4)));
        }
        return result;
    }

    public async Task<List<SubscriptionInfo>> GetSubscriptionsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);
        await using var cmd = new NpgsqlCommand(SubscriptionsQuery, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<SubscriptionInfo>();
        while (await reader.ReadAsync(ct))
        {
            var publications = reader.IsDBNull(4)
                ? []
                : (string[])reader.GetValue(4);

            result.Add(new SubscriptionInfo(
                Name: reader.GetString(0),
                Enabled: reader.GetBoolean(1),
                SlotName: reader.IsDBNull(2) ? null : reader.GetString(2),
                ConnInfo: reader.IsDBNull(3) ? null : reader.GetString(3),
                Publications: publications));
        }
        return result;
    }

    public async Task<List<SubscriptionStatInfo>> GetSubscriptionStatsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);
        await using var cmd = new NpgsqlCommand(SubscriptionStatsQuery, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<SubscriptionStatInfo>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new SubscriptionStatInfo(
                SubscriptionName: reader.GetString(0),
                WorkerRunning: reader.GetBoolean(1),
                LastMessageReceiptTime: reader.IsDBNull(2) ? null : new DateTimeOffset(reader.GetDateTime(2))));
        }
        return result;
    }

    public async Task<List<ReplicationStatInfo>> GetReplicationStatsAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);
        await using var cmd = new NpgsqlCommand(ReplicationStatsQuery, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<ReplicationStatInfo>();
        while (await reader.ReadAsync(ct))
        {
            result.Add(new ReplicationStatInfo(
                ApplicationName: reader.IsDBNull(0) ? null : reader.GetString(0),
                ClientAddress: reader.IsDBNull(1) ? null : reader.GetString(1),
                State: reader.IsDBNull(2) ? "unknown" : reader.GetString(2),
                SyncState: reader.IsDBNull(3) ? null : reader.GetString(3)));
        }
        return result;
    }

    public async Task<List<PublicationTableInfo>> GetPublicationTablesAsync(PgConnection connection, string plaintextPassword, IReadOnlyList<string> publicationNames, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);
        await using var cmd = new NpgsqlCommand(PublicationTablesQuery, conn);
        cmd.Parameters.AddWithValue("pubnames", publicationNames.ToArray());
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<PublicationTableInfo>();
        while (await reader.ReadAsync(ct))
            result.Add(new PublicationTableInfo(reader.GetString(0), reader.GetString(1)));
        return result;
    }

    public async Task<List<string>> GetTableColumnsAsync(PgConnection connection, string plaintextPassword, string schemaName, string tableName, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);
        await using var cmd = new NpgsqlCommand(TableColumnsQuery, conn);
        cmd.Parameters.AddWithValue("schema", schemaName);
        cmd.Parameters.AddWithValue("table", tableName);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<string>();
        while (await reader.ReadAsync(ct))
            result.Add(reader.GetString(0));
        return result;
    }

    public async Task<TableChecksum> GetTableChecksumAsync(PgConnection connection, string plaintextPassword, string schemaName, string tableName, IReadOnlyList<string> columnNames, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);
        var quotedColumns = columnNames.Select(QuoteIdentifier).ToList();
        var query = BuildTableChecksumQuery(QuoteIdentifier(schemaName), QuoteIdentifier(tableName), quotedColumns);
        await using var cmd = new NpgsqlCommand(query, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return new TableChecksum(0, "0");

        var rowCount = reader.GetInt64(0);
        var checksum = reader.GetString(1);
        return new TableChecksum(rowCount, checksum);
    }

    private static string QuoteIdentifier(string identifier) => $"\"{identifier.Replace("\"", "\"\"")}\"";

    private static async Task<NpgsqlConnection> OpenAsync(PgConnection connection, string plaintextPassword, CancellationToken ct)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = connection.Host,
            Port = connection.Port,
            Database = connection.DatabaseName,
            Username = connection.Username,
            Password = plaintextPassword,
            SslMode = PgSslModeMapper.ToNpgsql(connection.SslMode),
            TrustServerCertificate = connection.TrustServerCertificate,
            // CdcDiscoveryService, DiscoveryTimeoutSeconds'tan türettiği bir CancellationToken
            // geçirir; burada sabit düşük bir bağlantı/komut zaman aşımı olsaydı, admin'in
            // ayarladığı daha uzun bir değer sessizce görmezden gelinirdi. ReconciliationService
            // ise (büyük tablo checksum sorguları meşru biçimde uzun sürebileceğinden) sorgu
            // başına bir zaman aşımı geçirmiyor; bu yüzden komut zaman aşımı burada sınırlanmaz,
            // yalnızca bağlantı kurma adımına (Timeout) makul bir üst sınır konur — aksi halde
            // erişilemeyen bir sunucu tüm reconciliation/discovery döngüsünü süresiz kilitleyebilir.
            Timeout = 30,
            CommandTimeout = 0
        };

        var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
