using System.Collections.Concurrent;
using System.Diagnostics;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CdcMonitoring.Application.CdcDiscovery;

public class CdcDiscoveryService(
    IPgConnectionRepository connections,
    ICdcRelationshipRepository relationships,
    ICdcRelationshipHealthRepository relationshipHealth,
    IConnectionPasswordProtector passwordProtector,
    IPostgresInspector inspector,
    IMetricsRecorder metrics,
    IClock clock,
    ISystemSettingsRepository settingsRepository,
    ILogger<CdcDiscoveryService> logger)
{
    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var settings = await settingsRepository.GetAsync(ct);
        var active = await connections.GetActiveAsync(ct);

        var slotsByConnection = new ConcurrentDictionary<Guid, List<ReplicationSlotInfo>>();
        var subsByConnection = new ConcurrentDictionary<Guid, List<SubscriptionInfo>>();
        var subStatsByConnection = new ConcurrentDictionary<Guid, List<SubscriptionStatInfo>>();

        using var throttle = new SemaphoreSlim(Math.Max(1, settings.DiscoveryMaxDegreeOfParallelism));
        var collectTasks = active.Select(async connection =>
        {
            await throttle.WaitAsync(ct);
            try
            {
                await CollectAsync(connection, settings.DiscoveryTimeoutSeconds, slotsByConnection, subsByConnection, subStatsByConnection, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "CDC keşfi için bağlantı sorgulanamadı: {ConnectionName}", connection.Name);
            }
            finally
            {
                throttle.Release();
            }
        });
        await Task.WhenAll(collectTasks);

        foreach (var target in active)
        {
            if (!subsByConnection.TryGetValue(target.Id, out var subs))
                continue;

            foreach (var sub in subs)
            {
                await MatchAndPersistAsync(active, target, sub, slotsByConnection, subStatsByConnection, ct);
            }
        }

        stopwatch.Stop();
        metrics.RecordScanCycleDuration("cdc_discovery", stopwatch.Elapsed.TotalSeconds);
    }

    private async Task CollectAsync(
        PgConnection connection,
        int timeoutSeconds,
        ConcurrentDictionary<Guid, List<ReplicationSlotInfo>> slotsByConnection,
        ConcurrentDictionary<Guid, List<SubscriptionInfo>> subsByConnection,
        ConcurrentDictionary<Guid, List<SubscriptionStatInfo>> subStatsByConnection,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        var plaintextPassword = passwordProtector.Unprotect(connection.EncryptedPassword);

        slotsByConnection[connection.Id] = await inspector.GetReplicationSlotsAsync(connection, plaintextPassword, timeoutCts.Token);
        subsByConnection[connection.Id] = await inspector.GetSubscriptionsAsync(connection, plaintextPassword, timeoutCts.Token);
        subStatsByConnection[connection.Id] = await inspector.GetSubscriptionStatsAsync(connection, plaintextPassword, timeoutCts.Token);
    }

    private async Task MatchAndPersistAsync(
        List<PgConnection> active,
        PgConnection target,
        SubscriptionInfo sub,
        ConcurrentDictionary<Guid, List<ReplicationSlotInfo>> slotsByConnection,
        ConcurrentDictionary<Guid, List<SubscriptionStatInfo>> subStatsByConnection,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(sub.SlotName))
        {
            logger.LogWarning("Subscription '{Sub}' ({Target}) için slot adı yok, atlanıyor.", sub.Name, target.Name);
            return;
        }

        var parsed = ConnInfoParser.TryParse(sub.ConnInfo);
        if (parsed is null)
        {
            logger.LogWarning(
                "Subscription '{Sub}' ({Target}) için kaynak eşleştirilemedi — conninfo görünmüyor (redakte edilmiş olabilir). Manuel ilişki tanımlanabilir (FR-06).",
                sub.Name, target.Name);
            return;
        }

        var source = active.FirstOrDefault(c =>
            string.Equals(c.Host, parsed.Host, StringComparison.OrdinalIgnoreCase) &&
            c.Port == parsed.Port &&
            string.Equals(c.DatabaseName, parsed.Database, StringComparison.Ordinal));

        if (source is null)
        {
            logger.LogWarning(
                "Subscription '{Sub}' ({Target}) kayıtsız bir kaynağa işaret ediyor: {Host}:{Port}/{Db}. Bu bağlantıyı önce defterde kaydedin.",
                sub.Name, target.Name, parsed.Host, parsed.Port, parsed.Database);
            return;
        }

        var relationship = await relationships.FindAsync(source.Id, target.Id, sub.SlotName, ct);
        if (relationship is null)
        {
            relationship = new CdcRelationship
            {
                Id = Guid.NewGuid(),
                SourceConnectionId = source.Id,
                TargetConnectionId = target.Id,
                PublicationName = string.Join(",", sub.Publications),
                SubscriptionName = sub.Name,
                SlotName = sub.SlotName,
                Status = CdcRelationshipStatus.Inferred,
                CreatedAt = clock.UtcNow
            };
            await relationships.AddAsync(relationship, ct);
        }
        else
        {
            relationship.PublicationName = string.Join(",", sub.Publications);
        }

        await relationships.SaveChangesAsync(ct);

        var slotInfo = slotsByConnection.TryGetValue(source.Id, out var slots)
            ? slots.FirstOrDefault(s => s.SlotName == sub.SlotName)
            : null;

        var workerRunning = subStatsByConnection.TryGetValue(target.Id, out var stats) &&
            stats.Any(s => s.SubscriptionName == sub.Name && s.WorkerRunning);

        var subscriptionState = !sub.Enabled
            ? SubscriptionState.Disabled
            : workerRunning ? SubscriptionState.Enabled : SubscriptionState.Error;

        var lastSyncAt = stats?.FirstOrDefault(s => s.SubscriptionName == sub.Name)?.LastMessageReceiptTime;

        await relationshipHealth.AddAsync(new CdcRelationshipHealth
        {
            Id = Guid.NewGuid(),
            RelationshipId = relationship.Id,
            CheckedAt = clock.UtcNow,
            SlotActive = slotInfo?.Active ?? false,
            WalStatus = slotInfo?.WalStatus,
            LagBytes = slotInfo?.LagBytes,
            SubscriptionState = subscriptionState,
            LastSyncAt = lastSyncAt
        }, ct);
        await relationshipHealth.SaveChangesAsync(ct);

        metrics.RecordCdcRelationshipHealth(source.Name, target.Name, relationship.SlotName, slotInfo?.Active ?? false, slotInfo?.LagBytes, subscriptionState);

        if (subscriptionState == SubscriptionState.Error || slotInfo is { Active: false })
        {
            logger.LogWarning(
                "CDC ilişkisi sağlıksız: {Source} -> {Target} (slot: {Slot}, durum: {State}, slot aktif: {SlotActive})",
                source.Name, target.Name, relationship.SlotName, subscriptionState, slotInfo?.Active);
        }
    }
}
