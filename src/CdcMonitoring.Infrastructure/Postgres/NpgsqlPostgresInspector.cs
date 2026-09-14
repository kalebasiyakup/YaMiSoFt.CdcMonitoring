using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Application.SchemaCatalog;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
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
        SELECT column_name, data_type
        FROM information_schema.columns
        WHERE table_schema = @schema AND table_name = @table
        ORDER BY ordinal_position;
        """;

    // --- Şema kataloğu (FR-15) sorguları ---------------------------------------------
    // Hepsi tek seferde TÜM şemaları tarar; nesne başına sorgu açılmaz. Sistem şemaları
    // hem @excluded listesiyle hem de pg_ ön eki ile elenir (pg_temp_* / pg_toast_*
    // şemaları kullanıcı ayarına bırakılmayacak kadar gürültülüdür).

    internal const string CatalogTablesQuery = """
        SELECT
            n.nspname,
            c.relname,
            c.relkind::text,
            c.reltuples::bigint AS estimated_rows,
            pg_total_relation_size(c.oid) AS total_bytes,
            obj_description(c.oid, 'pg_class') AS table_comment
        FROM pg_catalog.pg_class c
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        WHERE c.relkind = ANY(ARRAY['r', 'p', 'v', 'm', 'f'])
          AND n.nspname <> ALL(@excluded)
          AND n.nspname NOT LIKE 'pg\_%'
        ORDER BY n.nspname, c.relname;
        """;

    // Kolonlar information_schema.columns yerine doğrudan pg_attribute'tan okunur:
    // information_schema.columns materyalize görünümlerin kolonlarını HİÇ göstermez (katalogda
    // matview'lar kolonsuz kalırdı) ve büyük kataloglarda belirgin biçimde yavaştır. Uzunluk/
    // hassasiyet için information_schema.columns'ın kendi kullandığı yardımcı fonksiyonlar
    // çağrılır; tip adı format_type ile (uzunluk eki olmadan) alınır, uzunluk ayrı kolonda durur.
    internal const string CatalogColumnsQuery = """
        SELECT
            n.nspname,
            c.relname,
            a.attname,
            a.attnum::int,
            format_type(a.atttypid, NULL) AS data_type,
            NOT a.attnotnull AS is_nullable,
            pg_get_expr(ad.adbin, ad.adrelid) AS column_default,
            information_schema._pg_char_max_length(a.atttypid, a.atttypmod)::int AS max_length,
            information_schema._pg_numeric_precision(a.atttypid, a.atttypmod)::int AS numeric_precision,
            information_schema._pg_numeric_scale(a.atttypid, a.atttypmod)::int AS numeric_scale,
            col_description(a.attrelid, a.attnum) AS column_comment
        FROM pg_catalog.pg_attribute a
        JOIN pg_catalog.pg_class c ON c.oid = a.attrelid
        JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
        LEFT JOIN pg_catalog.pg_attrdef ad ON ad.adrelid = a.attrelid AND ad.adnum = a.attnum
        WHERE a.attnum > 0
          AND NOT a.attisdropped
          AND c.relkind = ANY(ARRAY['r', 'p', 'v', 'm', 'f'])
          AND n.nspname <> ALL(@excluded)
          AND n.nspname NOT LIKE 'pg\_%'
        ORDER BY n.nspname, c.relname, a.attnum;
        """;

    // indkey'de 0 olan girdiler ifade (expression) indeksleridir ve pg_attribute'ta karşılığı
    // yoktur; bu durumda kolon listesi boş kalır, indeksin tam tanımı Definition alanında durur.
    internal const string CatalogIndexesQuery = """
        SELECT
            n.nspname,
            t.relname,
            i.relname AS index_name,
            ix.indisunique,
            ix.indisprimary,
            COALESCE((
                SELECT string_agg(att.attname, ',' ORDER BY k.ord)
                FROM unnest(ix.indkey) WITH ORDINALITY AS k(attnum, ord)
                JOIN pg_catalog.pg_attribute att ON att.attrelid = t.oid AND att.attnum = k.attnum
            ), '') AS index_columns,
            pg_get_indexdef(i.oid) AS index_definition,
            pg_relation_size(i.oid) AS index_bytes
        FROM pg_catalog.pg_index ix
        JOIN pg_catalog.pg_class i ON i.oid = ix.indexrelid
        JOIN pg_catalog.pg_class t ON t.oid = ix.indrelid
        JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace
        WHERE n.nspname <> ALL(@excluded)
          AND n.nspname NOT LIKE 'pg\_%'
        ORDER BY n.nspname, t.relname, i.relname;
        """;

    internal const string CatalogConstraintsQuery = """
        SELECT
            n.nspname,
            t.relname,
            con.conname,
            con.contype::text,
            COALESCE((
                SELECT string_agg(att.attname, ',' ORDER BY k.ord)
                FROM unnest(con.conkey) WITH ORDINALITY AS k(attnum, ord)
                JOIN pg_catalog.pg_attribute att ON att.attrelid = con.conrelid AND att.attnum = k.attnum
            ), '') AS constraint_columns,
            fn.nspname AS referenced_schema,
            ft.relname AS referenced_table,
            (
                SELECT string_agg(att.attname, ',' ORDER BY k.ord)
                FROM unnest(con.confkey) WITH ORDINALITY AS k(attnum, ord)
                JOIN pg_catalog.pg_attribute att ON att.attrelid = con.confrelid AND att.attnum = k.attnum
            ) AS referenced_columns,
            pg_get_constraintdef(con.oid) AS constraint_definition
        FROM pg_catalog.pg_constraint con
        JOIN pg_catalog.pg_class t ON t.oid = con.conrelid
        JOIN pg_catalog.pg_namespace n ON n.oid = t.relnamespace
        LEFT JOIN pg_catalog.pg_class ft ON ft.oid = con.confrelid
        LEFT JOIN pg_catalog.pg_namespace fn ON fn.oid = ft.relnamespace
        WHERE n.nspname <> ALL(@excluded)
          AND n.nspname NOT LIKE 'pg\_%'
        ORDER BY n.nspname, t.relname, con.conname;
        """;

    internal const string CatalogPublishedTablesQuery = """
        SELECT DISTINCT schemaname, tablename
        FROM pg_catalog.pg_publication_tables;
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

    public async Task<List<ColumnInfo>> GetTableColumnsAsync(PgConnection connection, string plaintextPassword, string schemaName, string tableName, CancellationToken ct = default)
    {
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);
        await using var cmd = new NpgsqlCommand(TableColumnsQuery, conn);
        cmd.Parameters.AddWithValue("schema", schemaName);
        cmd.Parameters.AddWithValue("table", tableName);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        var result = new List<ColumnInfo>();
        while (await reader.ReadAsync(ct))
            result.Add(new ColumnInfo(reader.GetString(0), reader.GetString(1)));
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

    public async Task<SchemaCatalogSnapshot> GetSchemaCatalogAsync(PgConnection connection, string plaintextPassword, IReadOnlyList<string> excludedSchemas, CancellationToken ct = default)
    {
        // Tüm katalog sorguları TEK bağlantı üzerinden sırayla çalışır: binlerce tablolu bir
        // veritabanında nesne başına bağlantı açmak hem izlenen sunucuyu hem taramayı boğar.
        var excluded = excludedSchemas.Count > 0 ? excludedSchemas.ToArray() : [string.Empty];
        await using var conn = await OpenAsync(connection, plaintextPassword, ct);

        var tables = new Dictionary<(string Schema, string Table), CatalogTableInfo>();

        await using (var cmd = new NpgsqlCommand(CatalogTablesQuery, conn))
        {
            cmd.Parameters.AddWithValue("excluded", excluded);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var schema = reader.GetString(0);
                var table = reader.GetString(1);
                // PG14+ hiç ANALYZE edilmemiş tablolar için reltuples = -1 döner; bunu
                // "0 satır" diye göstermek yanıltıcı olur, bilinmiyor (null) olarak saklanır.
                var estimatedRows = reader.IsDBNull(3) ? (long?)null : reader.GetInt64(3);
                tables[(schema, table)] = new CatalogTableInfo
                {
                    SchemaName = schema,
                    TableName = table,
                    Kind = MapObjectKind(reader.GetString(2)),
                    EstimatedRowCount = estimatedRows < 0 ? null : estimatedRows,
                    TotalSizeBytes = reader.IsDBNull(4) ? null : reader.GetInt64(4),
                    Comment = reader.IsDBNull(5) ? null : reader.GetString(5)
                };
            }
        }

        await using (var cmd = new NpgsqlCommand(CatalogColumnsQuery, conn))
        {
            cmd.Parameters.AddWithValue("excluded", excluded);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (!tables.TryGetValue((reader.GetString(0), reader.GetString(1)), out var table))
                    continue;

                table.Columns.Add(new CatalogColumnInfo(
                    reader.GetString(2),
                    reader.GetInt32(3),
                    reader.GetString(4),
                    reader.GetBoolean(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    reader.IsDBNull(8) ? null : reader.GetInt32(8),
                    reader.IsDBNull(9) ? null : reader.GetInt32(9),
                    reader.IsDBNull(10) ? null : reader.GetString(10)));
            }
        }

        await using (var cmd = new NpgsqlCommand(CatalogIndexesQuery, conn))
        {
            cmd.Parameters.AddWithValue("excluded", excluded);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (!tables.TryGetValue((reader.GetString(0), reader.GetString(1)), out var table))
                    continue;

                table.Indexes.Add(new CatalogIndexInfo(
                    reader.GetString(2),
                    reader.GetBoolean(3),
                    reader.GetBoolean(4),
                    reader.GetString(5),
                    reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetInt64(7)));
            }
        }

        await using (var cmd = new NpgsqlCommand(CatalogConstraintsQuery, conn))
        {
            cmd.Parameters.AddWithValue("excluded", excluded);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (!tables.TryGetValue((reader.GetString(0), reader.GetString(1)), out var table))
                    continue;

                table.Constraints.Add(new CatalogConstraintInfo(
                    reader.GetString(2),
                    MapConstraintKind(reader.GetString(3)),
                    reader.GetString(4),
                    reader.IsDBNull(5) ? null : reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7),
                    reader.GetString(8)));
            }
        }

        await using (var cmd = new NpgsqlCommand(CatalogPublishedTablesQuery, conn))
        {
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (tables.TryGetValue((reader.GetString(0), reader.GetString(1)), out var table))
                    table.IsPublished = true;
            }
        }

        // Kolonun PK'ya dahil olup olmadığı ayrı bir sorgu yerine PK kısıtının kolon
        // listesinden türetilir.
        foreach (var table in tables.Values)
        {
            var primaryKey = table.Constraints.FirstOrDefault(c => c.Kind == DbConstraintKind.PrimaryKey);
            if (primaryKey is null)
                continue;

            var pkColumns = primaryKey.ColumnsCsv
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToHashSet();

            foreach (var column in table.Columns.Where(c => pkColumns.Contains(c.Name)))
                column.IsPrimaryKey = true;
        }

        return new SchemaCatalogSnapshot([.. tables.Values]);
    }

    private static DbObjectKind MapObjectKind(string relkind) => relkind switch
    {
        "p" => DbObjectKind.PartitionedTable,
        "v" => DbObjectKind.View,
        "m" => DbObjectKind.MaterializedView,
        "f" => DbObjectKind.ForeignTable,
        _ => DbObjectKind.Table
    };

    private static DbConstraintKind MapConstraintKind(string contype) => contype switch
    {
        "p" => DbConstraintKind.PrimaryKey,
        "f" => DbConstraintKind.ForeignKey,
        "u" => DbConstraintKind.Unique,
        "c" => DbConstraintKind.Check,
        "x" => DbConstraintKind.Exclusion,
        _ => DbConstraintKind.Other
    };

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
