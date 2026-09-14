using CdcMonitoring.Application.SchemaCatalog;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using CdcMonitoring.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CdcMonitoring.UnitTests.SchemaCatalog;

public class SchemaCatalogServiceTests
{
    private static readonly DateTimeOffset FirstRun = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset SecondRun = new(2026, 9, 2, 8, 0, 0, TimeSpan.Zero);

    private static PgConnection NewConnection(string name = "orders-prod") => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Host = $"{name}.internal",
        Port = 5432,
        DatabaseName = "orders",
        Username = "monitor_ro",
        EncryptedPassword = "enc:s3cr3t!",
        EnvironmentTag = "Prod-DC1",
        IsActive = true,
        CreatedAt = FirstRun,
        CreatedBy = "system"
    };

    private static CatalogTableInfo Table(
        string schema, string table,
        IEnumerable<CatalogColumnInfo>? columns = null,
        IEnumerable<CatalogIndexInfo>? indexes = null,
        IEnumerable<CatalogConstraintInfo>? constraints = null,
        bool isPublished = false)
    {
        var info = new CatalogTableInfo
        {
            SchemaName = schema,
            TableName = table,
            Kind = DbObjectKind.Table,
            EstimatedRowCount = 100,
            TotalSizeBytes = 8192,
            IsPublished = isPublished
        };

        info.Columns.AddRange(columns ?? []);
        info.Indexes.AddRange(indexes ?? []);
        info.Constraints.AddRange(constraints ?? []);
        return info;
    }

    private static CatalogColumnInfo Column(string name, string dataType = "integer", bool isNullable = false, int ordinal = 1, string? defaultExpression = null) =>
        new(name, ordinal, dataType, isNullable, defaultExpression, null, null, null, null);

    private sealed record Harness(
        SchemaCatalogService Service,
        InMemoryPgConnectionRepository Connections,
        InMemorySchemaCatalogRepository Catalog,
        InMemorySchemaChangeEventRepository Changes,
        FakePostgresInspector Inspector,
        MutableClock Clock);

    private static Harness CreateHarness()
    {
        var connections = new InMemoryPgConnectionRepository();
        var catalog = new InMemorySchemaCatalogRepository();
        var changes = new InMemorySchemaChangeEventRepository();
        var inspector = new FakePostgresInspector();
        var clock = new MutableClock(FirstRun);

        var service = new SchemaCatalogService(
            connections, catalog, changes, new FakePasswordProtector(), inspector,
            new FakeSystemSettingsRepository(FakeSystemSettingsRepository.CreateDefault()),
            new RecordingMetricsRecorder(), clock,
            NullLogger<SchemaCatalogService>.Instance);

        return new Harness(service, connections, catalog, changes, inspector, clock);
    }

    [Fact]
    public async Task First_scan_populates_catalog_without_producing_change_events()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);

        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id"), Column("total", "numeric", ordinal: 2)])]);

        await h.Service.RunOnceAsync();

        var table = Assert.Single(h.Catalog.Tables);
        Assert.Equal("public", table.SchemaName);
        Assert.Equal("orders", table.TableName);
        Assert.Equal(2, table.Columns.Count);
        Assert.Equal(FirstRun, table.FirstSeenAt);
        Assert.Null(table.DroppedAt);

        // İlk doldurmada her tablo "yeni" görünür; binlerce sahte olay üretilmemeli.
        Assert.Empty(h.Changes.Events);

        var scan = Assert.Single(h.Catalog.Scans);
        Assert.True(scan.Success);
        Assert.Equal(1, scan.TableCount);
        Assert.Equal(2, scan.ColumnCount);
    }

    [Fact]
    public async Task Second_scan_without_changes_produces_no_events()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")])]);

        await h.Service.RunOnceAsync();
        h.Clock.UtcNow = SecondRun;
        await h.Service.RunOnceAsync();

        Assert.Empty(h.Changes.Events);
        Assert.Single(h.Catalog.Tables);
        Assert.Equal(SecondRun, h.Catalog.Tables[0].LastSeenAt);
    }

    [Fact]
    public async Task Added_column_is_stored_and_recorded_as_change()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")])]);
        await h.Service.RunOnceAsync();

        h.Clock.UtcNow = SecondRun;
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id"), Column("customer_id", ordinal: 2)])]);
        await h.Service.RunOnceAsync();

        var table = Assert.Single(h.Catalog.Tables);
        Assert.Equal(2, table.Columns.Count);

        var change = Assert.Single(h.Changes.Events);
        Assert.Equal(SchemaChangeType.ColumnAdded, change.ChangeType);
        Assert.Equal("customer_id", change.ObjectName);
        Assert.Equal("orders", change.TableName);
        Assert.Equal(SecondRun, change.DetectedAt);
    }

    [Fact]
    public async Task Dropped_column_is_marked_not_deleted()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id"), Column("legacy_code", "text", ordinal: 2)])]);
        await h.Service.RunOnceAsync();

        h.Clock.UtcNow = SecondRun;
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")])]);
        await h.Service.RunOnceAsync();

        var table = Assert.Single(h.Catalog.Tables);
        var legacy = Assert.Single(table.Columns, c => c.ColumnName == "legacy_code");
        Assert.Equal(SecondRun, legacy.DroppedAt);

        var change = Assert.Single(h.Changes.Events);
        Assert.Equal(SchemaChangeType.ColumnDropped, change.ChangeType);
        Assert.Equal("legacy_code", change.ObjectName);
        Assert.Equal("text", change.OldValue);
    }

    [Fact]
    public async Task Column_type_and_nullability_changes_are_recorded_with_old_and_new_values()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("total", "integer")])]);
        await h.Service.RunOnceAsync();

        h.Clock.UtcNow = SecondRun;
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("total", "numeric", isNullable: true)])]);
        await h.Service.RunOnceAsync();

        var typeChange = Assert.Single(h.Changes.Events, e => e.ChangeType == SchemaChangeType.ColumnTypeChanged);
        Assert.Equal("integer", typeChange.OldValue);
        Assert.Equal("numeric", typeChange.NewValue);

        var nullabilityChange = Assert.Single(h.Changes.Events, e => e.ChangeType == SchemaChangeType.ColumnNullabilityChanged);
        Assert.Equal("NOT NULL", nullabilityChange.OldValue);
        Assert.Equal("NULL", nullabilityChange.NewValue);

        var table = Assert.Single(h.Catalog.Tables);
        Assert.Equal("numeric", table.Columns[0].DataType);
        Assert.True(table.Columns[0].IsNullable);
    }

    [Fact]
    public async Task Dropped_table_produces_single_event_and_marks_children()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
        [
            Table("public", "orders", [Column("id")]),
            Table("public", "audit_tmp", [Column("id"), Column("payload", "jsonb", ordinal: 2)])
        ]);
        await h.Service.RunOnceAsync();

        h.Clock.UtcNow = SecondRun;
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")])]);
        await h.Service.RunOnceAsync();

        // Tablo düşüşü tek olay olarak kaydedilir; kolon başına ayrıca olay üretilmez.
        var change = Assert.Single(h.Changes.Events);
        Assert.Equal(SchemaChangeType.TableDropped, change.ChangeType);
        Assert.Equal("audit_tmp", change.TableName);
        Assert.Null(change.ObjectName);

        var dropped = Assert.Single(h.Catalog.Tables, t => t.TableName == "audit_tmp");
        Assert.Equal(SecondRun, dropped.DroppedAt);
        Assert.All(dropped.Columns, c => Assert.Equal(SecondRun, c.DroppedAt));
    }

    [Fact]
    public async Task Table_that_comes_back_is_revived_instead_of_duplicated()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")]), Table("public", "staging", [Column("id")])]);
        await h.Service.RunOnceAsync();

        h.Clock.UtcNow = SecondRun;
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")])]);
        await h.Service.RunOnceAsync();

        h.Clock.UtcNow = SecondRun.AddDays(1);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")]), Table("public", "staging", [Column("id")])]);
        await h.Service.RunOnceAsync();

        // Aynı (şema, tablo) için ikinci bir satır açılmamalı — DB'deki unique index bunu
        // zaten reddeder, katalog kaydı yeniden canlandırılmalı.
        Assert.Equal(2, h.Catalog.Tables.Count);
        var staging = Assert.Single(h.Catalog.Tables, t => t.TableName == "staging");
        Assert.Null(staging.DroppedAt);
        Assert.Equal(FirstRun, staging.FirstSeenAt);
        Assert.Equal(2, h.Changes.Events.Count(e => e.TableName == "staging"));
    }

    [Fact]
    public async Task Failed_scan_leaves_catalog_untouched_and_records_failure()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")])]);
        await h.Service.RunOnceAsync();

        h.Clock.UtcNow = SecondRun;
        h.Inspector.SchemaCatalogFailures.Add(connection.Id);
        await h.Service.RunOnceAsync();

        // Erişilemeyen sunucunun tabloları "silinmiş" sayılmamalı.
        var table = Assert.Single(h.Catalog.Tables);
        Assert.Null(table.DroppedAt);
        Assert.Equal(FirstRun, table.LastSeenAt);
        Assert.Empty(h.Changes.Events);

        var failedScan = Assert.Single(h.Catalog.Scans, s => !s.Success);
        Assert.Equal(SecondRun, failedScan.StartedAt);
        Assert.Contains("Katalog taraması başarısız", failedScan.ErrorMessage);
    }

    [Fact]
    public async Task One_failing_connection_does_not_block_the_others()
    {
        var h = CreateHarness();
        var healthy = NewConnection("orders-prod");
        var broken = NewConnection("orders-dr");
        await h.Connections.AddAsync(healthy);
        await h.Connections.AddAsync(broken);

        h.Inspector.SchemaCatalogs[healthy.Id] = new SchemaCatalogSnapshot([Table("public", "orders", [Column("id")])]);
        h.Inspector.SchemaCatalogFailures.Add(broken.Id);

        await h.Service.RunOnceAsync();

        Assert.Single(h.Catalog.Tables);
        Assert.Equal(2, h.Catalog.Scans.Count);
        Assert.Single(h.Catalog.Scans, s => s.Success);
        Assert.Single(h.Catalog.Scans, s => !s.Success);
    }

    [Fact]
    public async Task Index_and_constraint_changes_are_recorded()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);

        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
        [
            Table("public", "orders", [Column("id")],
                indexes: [new CatalogIndexInfo("orders_pkey", true, true, "id", "INDEX orders_pkey ON public.orders USING btree (id)", 4096)],
                constraints: [new CatalogConstraintInfo("orders_pkey", DbConstraintKind.PrimaryKey, "id", null, null, null, "PRIMARY KEY (id)")])
        ]);
        await h.Service.RunOnceAsync();

        h.Clock.UtcNow = SecondRun;
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
        [
            Table("public", "orders", [Column("id"), Column("customer_id", ordinal: 2)],
                indexes:
                [
                    new CatalogIndexInfo("orders_pkey", true, true, "id", "INDEX orders_pkey ON public.orders USING btree (id)", 4096),
                    new CatalogIndexInfo("orders_customer_idx", false, false, "customer_id", "INDEX orders_customer_idx ON public.orders USING btree (customer_id)", 2048)
                ],
                constraints:
                [
                    new CatalogConstraintInfo("orders_pkey", DbConstraintKind.PrimaryKey, "id", null, null, null, "PRIMARY KEY (id)"),
                    new CatalogConstraintInfo("orders_customer_fk", DbConstraintKind.ForeignKey, "customer_id", "public", "customers", "id",
                        "FOREIGN KEY (customer_id) REFERENCES customers(id)")
                ])
        ]);
        await h.Service.RunOnceAsync();

        Assert.Single(h.Changes.Events, e => e.ChangeType == SchemaChangeType.IndexAdded && e.ObjectName == "orders_customer_idx");
        var fk = Assert.Single(h.Changes.Events, e => e.ChangeType == SchemaChangeType.ConstraintAdded);
        Assert.Equal("orders_customer_fk", fk.ObjectName);

        var table = Assert.Single(h.Catalog.Tables);
        Assert.Equal(2, table.Indexes.Count);
        var storedFk = Assert.Single(table.Constraints, c => c.Kind == DbConstraintKind.ForeignKey);
        Assert.Equal("customers", storedFk.ReferencedTable);
        Assert.Equal("public", storedFk.ReferencedSchema);
    }

    [Fact]
    public async Task Size_and_publication_flags_are_refreshed_without_change_events()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")])]);
        await h.Service.RunOnceAsync();

        h.Clock.UtcNow = SecondRun;
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")], isPublished: true)]);
        await h.Service.RunOnceAsync();

        var table = Assert.Single(h.Catalog.Tables);
        Assert.True(table.IsPublished);

        // Boyut/satır sayısı/publication üyeliği her taramada değişebilir; bunlar şema
        // değişikliği sayılmaz, aksi halde günlük gürültüden okunamaz hale gelir.
        Assert.Empty(h.Changes.Events);
    }

    [Fact]
    public async Task Excluded_schemas_setting_is_passed_to_the_inspector()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot([]);

        await h.Service.RunOnceAsync();

        Assert.Equal(["pg_catalog", "information_schema", "pg_toast"], h.Inspector.LastExcludedSchemas);
    }

    [Fact]
    public async Task Manual_scan_runs_for_a_single_connection_including_inactive_ones()
    {
        var h = CreateHarness();
        var connection = NewConnection();
        connection.IsActive = false;
        await h.Connections.AddAsync(connection);
        h.Inspector.SchemaCatalogs[connection.Id] = new SchemaCatalogSnapshot(
            [Table("public", "orders", [Column("id")])]);

        var scan = await h.Service.RunForConnectionAsync(connection.Id);

        Assert.True(scan.Success);
        Assert.Equal(1, scan.TableCount);
        Assert.Single(h.Catalog.Tables);
    }
}
