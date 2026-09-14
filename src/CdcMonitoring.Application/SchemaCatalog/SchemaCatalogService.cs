using System.Collections.Concurrent;
using System.Diagnostics;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CdcMonitoring.Application.SchemaCatalog;

/// <summary>
/// Kayıtlı her bağlantının şema kataloğunu (tablo/kolon/indeks/kısıt) periyodik olarak
/// salt-okuma ile toplar ve cdc_monitoring veritabanında "güncel durum" olarak saklar
/// (FR-15). Her tarama bir öncekiyle karşılaştırılır; farklar SchemaChangeEvent olarak
/// biriktirilir (FR-16). Katalog kayıtları silinmez, kaynakta kalmayan nesneler DroppedAt
/// ile işaretlenir — böylece "bu kolon ne zaman kayboldu" sorusu cevaplanabilir.
/// İzlenen PostgreSQL örneklerinde DDL/DML çalıştırılmaz (FR-14).
/// </summary>
public class SchemaCatalogService(
    IPgConnectionRepository connections,
    ISchemaCatalogRepository catalog,
    ISchemaChangeEventRepository changeEvents,
    IConnectionPasswordProtector passwordProtector,
    IPostgresInspector inspector,
    ISystemSettingsRepository settingsRepository,
    IMetricsRecorder metrics,
    IClock clock,
    ILogger<SchemaCatalogService> logger)
{
    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var settings = await settingsRepository.GetAsync(ct);
        var active = await connections.GetActiveAsync(ct);
        var excluded = ParseCsv(settings.SchemaCatalogExcludedSchemasCsv);

        // Uzak sorgular paralel, yazma sıralı: EF DbContext (scoped) eşzamanlı kullanılamaz.
        var outcomes = new ConcurrentBag<CollectOutcome>();
        using var throttle = new SemaphoreSlim(Math.Max(1, settings.SchemaCatalogMaxDegreeOfParallelism));

        await Task.WhenAll(active.Select(async connection =>
        {
            await throttle.WaitAsync(ct);
            try
            {
                outcomes.Add(await CollectAsync(connection, excluded, settings.SchemaCatalogTimeoutSeconds, ct));
            }
            finally
            {
                throttle.Release();
            }
        }));

        foreach (var outcome in outcomes)
        {
            var connection = active.First(c => c.Id == outcome.ConnectionId);
            await PersistAsync(connection, outcome, ct);
        }

        stopwatch.Stop();
        metrics.RecordScanCycleDuration("schema_catalog", stopwatch.Elapsed.TotalSeconds);
    }

    /// <summary>
    /// Katalog ekranındaki "Şimdi Tara" düğmesi için: tek bağlantıyı talep üzerine tarar.
    /// Pasif bağlantılar da taranabilir — kullanıcı açıkça istemiştir.
    /// </summary>
    public async Task<SchemaScan> RunForConnectionAsync(Guid connectionId, CancellationToken ct = default)
    {
        var settings = await settingsRepository.GetAsync(ct);
        var connection = await connections.GetByIdAsync(connectionId, ct)
            ?? throw new KeyNotFoundException($"Bağlantı bulunamadı: {connectionId}");

        var outcome = await CollectAsync(connection, ParseCsv(settings.SchemaCatalogExcludedSchemasCsv),
            settings.SchemaCatalogTimeoutSeconds, ct);

        return await PersistAsync(connection, outcome, ct);
    }

    private async Task<CollectOutcome> CollectAsync(PgConnection connection, IReadOnlyList<string> excludedSchemas, int timeoutSeconds, CancellationToken ct)
    {
        var startedAt = clock.UtcNow;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            var password = passwordProtector.Unprotect(connection.EncryptedPassword);
            var snapshot = await inspector.GetSchemaCatalogAsync(connection, password, excludedSchemas, timeoutCts.Token);

            stopwatch.Stop();
            return new CollectOutcome(connection.Id, snapshot, null, startedAt, clock.UtcNow, stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogWarning(ex, "Şema kataloğu taranamadı: {ConnectionName}", connection.Name);
            return new CollectOutcome(connection.Id, null, ex.Message, startedAt, clock.UtcNow, stopwatch.Elapsed.TotalMilliseconds);
        }
    }

    private async Task<SchemaScan> PersistAsync(PgConnection connection, CollectOutcome outcome, CancellationToken ct)
    {
        var scan = new SchemaScan
        {
            Id = Guid.NewGuid(),
            ConnectionId = connection.Id,
            StartedAt = outcome.StartedAt,
            CompletedAt = outcome.CompletedAt,
            Success = outcome.Snapshot is not null,
            ErrorMessage = outcome.Error,
            DurationMs = outcome.DurationMs
        };

        // Başarısız tarama katalog verisine DOKUNMAZ: erişilemeyen bir sunucunun tüm
        // tabloları "silinmiş" olarak işaretlenirse hem katalog bozulur hem de sunucu geri
        // geldiğinde binlerce sahte "tablo eklendi" değişikliği üretilir.
        if (outcome.Snapshot is not null)
        {
            var (tableCount, columnCount) = await ApplyAsync(connection, outcome.Snapshot, outcome.CompletedAt, ct);
            scan.TableCount = tableCount;
            scan.ColumnCount = columnCount;
        }

        await catalog.AddScanAsync(scan, ct);
        await catalog.SaveChangesAsync(ct);
        return scan;
    }

    private async Task<(int TableCount, int ColumnCount)> ApplyAsync(PgConnection connection, SchemaCatalogSnapshot snapshot, DateTimeOffset now, CancellationToken ct)
    {
        var existing = await catalog.GetTablesForDiffAsync(connection.Id, ct);
        var existingByKey = existing.ToDictionary(t => (t.SchemaName, t.TableName));

        // İlk tarama katalogun tamamını "yeni" görür; binlerce TableAdded kaydı değişiklik
        // günlüğünü kullanılamaz hale getireceği için ilk doldurmada olay üretilmez.
        var isInitialScan = existing.Count == 0;
        var events = new List<SchemaChangeEvent>();
        var seen = new HashSet<(string, string)>();

        foreach (var incoming in snapshot.Tables)
        {
            var key = (incoming.SchemaName, incoming.TableName);
            seen.Add(key);

            if (!existingByKey.TryGetValue(key, out var table))
            {
                table = new DbTable
                {
                    Id = Guid.NewGuid(),
                    ConnectionId = connection.Id,
                    SchemaName = incoming.SchemaName,
                    TableName = incoming.TableName,
                    FirstSeenAt = now
                };
                await catalog.AddTableAsync(table, ct);
                AddEvent(events, connection.Id, incoming.SchemaName, incoming.TableName, null, SchemaChangeType.TableAdded, null, null, now, isInitialScan);

                // Yeni tablonun kolon/indeks/kısıtları ayrıca "eklendi" olayı üretmez —
                // tablonun kendisi zaten tek bir olayla kaydedildi.
                foreach (var column in incoming.Columns)
                    table.Columns.Add(CreateColumn(column, now));
                foreach (var index in incoming.Indexes)
                    table.Indexes.Add(CreateIndex(index, now));
                foreach (var constraint in incoming.Constraints)
                    table.Constraints.Add(CreateConstraint(constraint, now));
            }
            else
            {
                // Geri gelen bir tablo tek bir TableAdded olayı üretir; düşerken alt nesneleri
                // için olay üretilmediği gibi, geri gelirken de kolon/indeks/kısıt başına
                // "eklendi" kaydı üretilmez.
                var revived = table.DroppedAt is not null;
                if (revived)
                {
                    table.DroppedAt = null;
                    AddEvent(events, connection.Id, incoming.SchemaName, incoming.TableName, null, SchemaChangeType.TableAdded, null, null, now, isInitialScan);
                }

                var suppressChildEvents = isInitialScan || revived;
                DiffColumns(table, incoming, connection.Id, events, now, suppressChildEvents);
                DiffIndexes(table, incoming, connection.Id, events, now, suppressChildEvents);
                DiffConstraints(table, incoming, connection.Id, events, now, suppressChildEvents);
            }

            table.Kind = incoming.Kind;
            table.EstimatedRowCount = incoming.EstimatedRowCount;
            table.TotalSizeBytes = incoming.TotalSizeBytes;
            table.Comment = incoming.Comment;
            table.IsPublished = incoming.IsPublished;
            table.HasPrimaryKey = incoming.HasPrimaryKey;
            table.LastSeenAt = now;
        }

        foreach (var table in existing.Where(t => t.DroppedAt is null && !seen.Contains((t.SchemaName, t.TableName))))
        {
            table.DroppedAt = now;
            AddEvent(events, connection.Id, table.SchemaName, table.TableName, null, SchemaChangeType.TableDropped, null, null, now, isInitialScan);

            // Alt nesneler için ayrı olay üretilmez: tablonun düşmesi tek bir olay olarak
            // anlamlıdır, kolon başına kayıt yalnızca gürültü olur.
            foreach (var column in table.Columns.Where(c => c.DroppedAt is null))
                column.DroppedAt = now;
            foreach (var index in table.Indexes.Where(i => i.DroppedAt is null))
                index.DroppedAt = now;
            foreach (var constraint in table.Constraints.Where(c => c.DroppedAt is null))
                constraint.DroppedAt = now;
        }

        if (events.Count > 0)
            await changeEvents.AddRangeAsync(events, ct);

        return (snapshot.Tables.Count, snapshot.ColumnCount);
    }

    private static void DiffColumns(DbTable table, CatalogTableInfo incoming, Guid connectionId, List<SchemaChangeEvent> events, DateTimeOffset now, bool suppressEvents)
    {
        var existingByName = table.Columns.ToDictionary(c => c.ColumnName);
        var seen = new HashSet<string>();

        foreach (var column in incoming.Columns)
        {
            seen.Add(column.Name);

            if (!existingByName.TryGetValue(column.Name, out var stored))
            {
                table.Columns.Add(CreateColumn(column, now));
                AddEvent(events, connectionId, table.SchemaName, table.TableName, column.Name, SchemaChangeType.ColumnAdded, null, column.DataType, now, suppressEvents);
                continue;
            }

            if (stored.DroppedAt is not null)
            {
                stored.DroppedAt = null;
                AddEvent(events, connectionId, table.SchemaName, table.TableName, column.Name, SchemaChangeType.ColumnAdded, null, column.DataType, now, suppressEvents);
            }
            else
            {
                if (!string.Equals(stored.DataType, column.DataType, StringComparison.Ordinal))
                    AddEvent(events, connectionId, table.SchemaName, table.TableName, column.Name, SchemaChangeType.ColumnTypeChanged, stored.DataType, column.DataType, now, suppressEvents);

                if (stored.IsNullable != column.IsNullable)
                    AddEvent(events, connectionId, table.SchemaName, table.TableName, column.Name, SchemaChangeType.ColumnNullabilityChanged,
                        stored.IsNullable ? "NULL" : "NOT NULL", column.IsNullable ? "NULL" : "NOT NULL", now, suppressEvents);

                if (!string.Equals(stored.DefaultExpression, column.DefaultExpression, StringComparison.Ordinal))
                    AddEvent(events, connectionId, table.SchemaName, table.TableName, column.Name, SchemaChangeType.ColumnDefaultChanged,
                        stored.DefaultExpression, column.DefaultExpression, now, suppressEvents);
            }

            stored.OrdinalPosition = column.OrdinalPosition;
            stored.DataType = column.DataType;
            stored.IsNullable = column.IsNullable;
            stored.DefaultExpression = column.DefaultExpression;
            stored.MaxLength = column.MaxLength;
            stored.NumericPrecision = column.NumericPrecision;
            stored.NumericScale = column.NumericScale;
            stored.IsPrimaryKey = column.IsPrimaryKey;
            stored.Comment = column.Comment;
            stored.LastSeenAt = now;
        }

        foreach (var stored in table.Columns.Where(c => c.DroppedAt is null && !seen.Contains(c.ColumnName)))
        {
            stored.DroppedAt = now;
            AddEvent(events, connectionId, table.SchemaName, table.TableName, stored.ColumnName, SchemaChangeType.ColumnDropped, stored.DataType, null, now, suppressEvents);
        }
    }

    private static void DiffIndexes(DbTable table, CatalogTableInfo incoming, Guid connectionId, List<SchemaChangeEvent> events, DateTimeOffset now, bool suppressEvents)
    {
        var existingByName = table.Indexes.ToDictionary(i => i.IndexName);
        var seen = new HashSet<string>();

        foreach (var index in incoming.Indexes)
        {
            seen.Add(index.Name);

            if (!existingByName.TryGetValue(index.Name, out var stored))
            {
                table.Indexes.Add(CreateIndex(index, now));
                AddEvent(events, connectionId, table.SchemaName, table.TableName, index.Name, SchemaChangeType.IndexAdded, null, index.Definition, now, suppressEvents);
                continue;
            }

            if (stored.DroppedAt is not null)
            {
                stored.DroppedAt = null;
                AddEvent(events, connectionId, table.SchemaName, table.TableName, index.Name, SchemaChangeType.IndexAdded, null, index.Definition, now, suppressEvents);
            }
            else if (!string.Equals(stored.Definition, index.Definition, StringComparison.Ordinal))
            {
                AddEvent(events, connectionId, table.SchemaName, table.TableName, index.Name, SchemaChangeType.IndexChanged, stored.Definition, index.Definition, now, suppressEvents);
            }

            stored.IsUnique = index.IsUnique;
            stored.IsPrimary = index.IsPrimary;
            stored.ColumnsCsv = index.ColumnsCsv;
            stored.Definition = index.Definition;
            stored.SizeBytes = index.SizeBytes;
            stored.LastSeenAt = now;
        }

        foreach (var stored in table.Indexes.Where(i => i.DroppedAt is null && !seen.Contains(i.IndexName)))
        {
            stored.DroppedAt = now;
            AddEvent(events, connectionId, table.SchemaName, table.TableName, stored.IndexName, SchemaChangeType.IndexDropped, stored.Definition, null, now, suppressEvents);
        }
    }

    private static void DiffConstraints(DbTable table, CatalogTableInfo incoming, Guid connectionId, List<SchemaChangeEvent> events, DateTimeOffset now, bool suppressEvents)
    {
        var existingByName = table.Constraints.ToDictionary(c => c.ConstraintName);
        var seen = new HashSet<string>();

        foreach (var constraint in incoming.Constraints)
        {
            seen.Add(constraint.Name);

            if (!existingByName.TryGetValue(constraint.Name, out var stored))
            {
                table.Constraints.Add(CreateConstraint(constraint, now));
                AddEvent(events, connectionId, table.SchemaName, table.TableName, constraint.Name, SchemaChangeType.ConstraintAdded, null, constraint.Definition, now, suppressEvents);
                continue;
            }

            if (stored.DroppedAt is not null)
            {
                stored.DroppedAt = null;
                AddEvent(events, connectionId, table.SchemaName, table.TableName, constraint.Name, SchemaChangeType.ConstraintAdded, null, constraint.Definition, now, suppressEvents);
            }
            else if (!string.Equals(stored.Definition, constraint.Definition, StringComparison.Ordinal))
            {
                AddEvent(events, connectionId, table.SchemaName, table.TableName, constraint.Name, SchemaChangeType.ConstraintChanged, stored.Definition, constraint.Definition, now, suppressEvents);
            }

            stored.Kind = constraint.Kind;
            stored.ColumnsCsv = constraint.ColumnsCsv;
            stored.ReferencedSchema = constraint.ReferencedSchema;
            stored.ReferencedTable = constraint.ReferencedTable;
            stored.ReferencedColumnsCsv = constraint.ReferencedColumnsCsv;
            stored.Definition = constraint.Definition;
            stored.LastSeenAt = now;
        }

        foreach (var stored in table.Constraints.Where(c => c.DroppedAt is null && !seen.Contains(c.ConstraintName)))
        {
            stored.DroppedAt = now;
            AddEvent(events, connectionId, table.SchemaName, table.TableName, stored.ConstraintName, SchemaChangeType.ConstraintDropped, stored.Definition, null, now, suppressEvents);
        }
    }

    // Alt nesnelerin Id'si bilinçli olarak atanmaz: bunlar DbContext'e açıkça Add edilmez,
    // takip edilen bir DbTable'ın koleksiyonuna eklenerek keşfedilir. EF, koleksiyonda bulduğu
    // bir nesnenin anahtarı DOLUYSA onu "var olan satır" kabul edip INSERT yerine UPDATE üretir
    // (ve satır olmadığı için DbUpdateConcurrencyException atar). Anahtar boş bırakıldığında
    // EF hem durumu Added işaretler hem de Guid'i kendisi üretir.
    private static DbColumn CreateColumn(CatalogColumnInfo column, DateTimeOffset now) => new()
    {
        ColumnName = column.Name,
        OrdinalPosition = column.OrdinalPosition,
        DataType = column.DataType,
        IsNullable = column.IsNullable,
        DefaultExpression = column.DefaultExpression,
        MaxLength = column.MaxLength,
        NumericPrecision = column.NumericPrecision,
        NumericScale = column.NumericScale,
        IsPrimaryKey = column.IsPrimaryKey,
        Comment = column.Comment,
        FirstSeenAt = now,
        LastSeenAt = now
    };

    private static DbIndex CreateIndex(CatalogIndexInfo index, DateTimeOffset now) => new()
    {
        IndexName = index.Name,
        IsUnique = index.IsUnique,
        IsPrimary = index.IsPrimary,
        ColumnsCsv = index.ColumnsCsv,
        Definition = index.Definition,
        SizeBytes = index.SizeBytes,
        FirstSeenAt = now,
        LastSeenAt = now
    };

    private static DbConstraint CreateConstraint(CatalogConstraintInfo constraint, DateTimeOffset now) => new()
    {
        ConstraintName = constraint.Name,
        Kind = constraint.Kind,
        ColumnsCsv = constraint.ColumnsCsv,
        ReferencedSchema = constraint.ReferencedSchema,
        ReferencedTable = constraint.ReferencedTable,
        ReferencedColumnsCsv = constraint.ReferencedColumnsCsv,
        Definition = constraint.Definition,
        FirstSeenAt = now,
        LastSeenAt = now
    };

    private static void AddEvent(
        List<SchemaChangeEvent> events, Guid connectionId, string schemaName, string tableName,
        string? objectName, SchemaChangeType changeType, string? oldValue, string? newValue,
        DateTimeOffset now, bool suppressEvents)
    {
        if (suppressEvents)
            return;

        events.Add(new SchemaChangeEvent
        {
            Id = Guid.NewGuid(),
            ConnectionId = connectionId,
            SchemaName = schemaName,
            TableName = tableName,
            ObjectName = objectName,
            ChangeType = changeType,
            OldValue = oldValue,
            NewValue = newValue,
            DetectedAt = now
        });
    }

    private static List<string> ParseCsv(string csv) =>
        [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    private record CollectOutcome(
        Guid ConnectionId,
        SchemaCatalogSnapshot? Snapshot,
        string? Error,
        DateTimeOffset StartedAt,
        DateTimeOffset CompletedAt,
        double DurationMs);
}
