using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CdcMonitoring.Application.Alerting;

/// <summary>
/// BRD §8'deki eşik tablosunun kod karşılığı. Sistem hiçbir koşulda otomatik
/// düzeltici aksiyon almaz (FR-14/§4.3) — tek çıktısı e-posta bildirimidir.
/// </summary>
public class AlertEvaluationService(
    IPgConnectionRepository connections,
    IConnectionHealthCheckRepository healthChecks,
    ICdcRelationshipRepository relationships,
    ICdcRelationshipHealthRepository relationshipHealth,
    IAlertEventRepository alertEvents,
    IEmailNotifier emailNotifier,
    IClock clock,
    ISystemSettingsRepository settingsRepository,
    ILogger<AlertEvaluationService> logger)
{
    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        var settings = await settingsRepository.GetAsync(ct);
        await EvaluateConnectionHealthChecksAsync(settings, ct);
        await EvaluateCdcRelationshipsAsync(settings, ct);
    }

    private async Task EvaluateConnectionHealthChecksAsync(SystemSettings settings, CancellationToken ct)
    {
        var active = await connections.GetActiveAsync(ct);
        foreach (var connection in active)
        {
            var recent = await healthChecks.GetRecentAsync(connection.Id, settings.ConsecutiveHealthCheckFailures, ct);
            var conditionActive = recent.Count == settings.ConsecutiveHealthCheckFailures && recent.All(c => !c.IsUp);

            await EvaluateRuleAsync(
                AlertType.HealthCheckFailed, AlertSeverity.Warning, connection.Id, relationshipId: null,
                conditionActive, TimeSpan.Zero,
                () => $"Bağlantı '{connection.Name}' ({connection.Host}:{connection.Port}) son {settings.ConsecutiveHealthCheckFailures} denemede erişilemedi.",
                ct);
        }
    }

    private async Task EvaluateCdcRelationshipsAsync(SystemSettings settings, CancellationToken ct)
    {
        var healthyWalStatuses = settings.HealthyWalStatusesCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var all = await relationships.GetAllAsync(ct);
        var trusted = all.Where(r => r.Status is CdcRelationshipStatus.Confirmed or CdcRelationshipStatus.Manual);

        foreach (var r in trusted)
        {
            var latest = await relationshipHealth.GetLatestAsync(r.Id, ct);
            if (latest is null) continue;

            var sourceName = r.SourceConnection?.Name ?? r.SourceConnectionId.ToString();
            var targetName = r.TargetConnection?.Name ?? r.TargetConnectionId.ToString();

            await EvaluateRuleAsync(
                AlertType.SlotInactive, AlertSeverity.Critical, connectionId: null, r.Id,
                conditionActive: !latest.SlotActive,
                TimeSpan.FromMinutes(settings.SlotInactiveMinutes),
                () => $"{sourceName} -> {targetName}: replication slot '{r.SlotName}' inaktif.",
                ct);

            var walHealthy = latest.WalStatus is null || healthyWalStatuses.Contains(latest.WalStatus);
            await EvaluateRuleAsync(
                AlertType.WalCritical, AlertSeverity.Critical, connectionId: null, r.Id,
                conditionActive: !walHealthy,
                TimeSpan.Zero,
                () => $"{sourceName} -> {targetName}: slot '{r.SlotName}' için WAL durumu kritik ({latest.WalStatus}).",
                ct);

            await EvaluateRuleAsync(
                AlertType.SubscriptionError, AlertSeverity.Critical, connectionId: null, r.Id,
                conditionActive: latest.SubscriptionState is SubscriptionState.Disabled or SubscriptionState.Error,
                TimeSpan.Zero,
                () => $"{sourceName} -> {targetName}: subscription '{r.SubscriptionName}' durumu {latest.SubscriptionState}.",
                ct);

            var since = clock.UtcNow - TimeSpan.FromMinutes(settings.LagWarningSustainedMinutes);
            var recentHealth = await relationshipHealth.GetSinceAsync(r.Id, since, ct);
            var lagConditionActive = recentHealth.Count > 0 &&
                recentHealth.All(h => h.LagBytes is not null && h.LagBytes >= settings.LagWarningBytes);

            // Sustained pencere kontrolü zaten yukarıda (since/recentHealth) uygulandı; burada
            // requiredSustainedDuration'ı tekrar LagWarningSustainedMinutes yapmak bildirimi
            // yanlışlıkla ~2 katı geciktirirdi (diğer kurallarla tutarsız — bkz. SlotInactive/
            // WalCritical/SubscriptionError, onlar sustain'i yalnızca bir kez uygular).
            await EvaluateRuleAsync(
                AlertType.LagWarning, AlertSeverity.Warning, connectionId: null, r.Id,
                conditionActive: lagConditionActive,
                TimeSpan.Zero,
                () => $"{sourceName} -> {targetName}: slot '{r.SlotName}' lag'i {latest.LagBytes:N0} byte, {settings.LagWarningSustainedMinutes} dk'dır eşik üstünde.",
                ct);
        }
    }

    private async Task EvaluateRuleAsync(
        AlertType type, AlertSeverity severity, Guid? connectionId, Guid? relationshipId,
        bool conditionActive, TimeSpan requiredSustainedDuration, Func<string> messageFactory, CancellationToken ct)
    {
        var active = await alertEvents.GetActiveAsync(type, connectionId, relationshipId, ct);

        if (!conditionActive)
        {
            if (active is not null)
            {
                active.ResolvedAt = clock.UtcNow;
                await alertEvents.SaveChangesAsync(ct);
            }
            return;
        }

        var now = clock.UtcNow;
        if (active is null)
        {
            active = new AlertEvent
            {
                Id = Guid.NewGuid(),
                Type = type,
                Severity = severity,
                ConnectionId = connectionId,
                RelationshipId = relationshipId,
                TriggeredAt = now,
                Message = messageFactory()
            };
            await alertEvents.AddAsync(active, ct);
            await alertEvents.SaveChangesAsync(ct);
        }

        if (active.NotifiedAt is null && now - active.TriggeredAt >= requiredSustainedDuration)
        {
            try
            {
                await emailNotifier.SendAlertAsync(severity, messageFactory(), ct);
                active.NotifiedAt = now;
                await alertEvents.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Alarm e-postası gönderilemedi: {Type} {ConnectionId} {RelationshipId}", type, connectionId, relationshipId);
            }
        }
    }
}
