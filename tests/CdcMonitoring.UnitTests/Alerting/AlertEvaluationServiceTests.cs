using CdcMonitoring.Application.Alerting;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using CdcMonitoring.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CdcMonitoring.UnitTests.Alerting;

public class AlertEvaluationServiceTests
{
    private static PgConnection NewConnection(string name) => new()
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
        CreatedAt = DateTimeOffset.UtcNow,
        CreatedBy = "system"
    };

    private static CdcRelationship NewConfirmedRelationship(Guid sourceId, Guid targetId) => new()
    {
        Id = Guid.NewGuid(),
        SourceConnectionId = sourceId,
        TargetConnectionId = targetId,
        PublicationName = "pub_orders",
        SubscriptionName = "sub_orders",
        SlotName = "slot_orders",
        Status = CdcRelationshipStatus.Confirmed,
        CreatedAt = DateTimeOffset.UtcNow
    };

    private static (AlertEvaluationService Service, InMemoryPgConnectionRepository Connections, InMemoryConnectionHealthCheckRepository HealthChecks,
        InMemoryCdcRelationshipRepository Relationships, InMemoryCdcRelationshipHealthRepository RelationshipHealth,
        InMemoryAlertEventRepository AlertEvents, FakeEmailNotifier Email, MutableClock Clock) CreateService(Action<SystemSettings>? configureSettings = null)
    {
        var connections = new InMemoryPgConnectionRepository();
        var healthChecks = new InMemoryConnectionHealthCheckRepository();
        var relationships = new InMemoryCdcRelationshipRepository();
        var relationshipHealth = new InMemoryCdcRelationshipHealthRepository();
        var alertEvents = new InMemoryAlertEventRepository();
        var email = new FakeEmailNotifier();
        var clock = new MutableClock(DateTimeOffset.Parse("2026-09-12T10:00:00Z"));

        var settings = FakeSystemSettingsRepository.CreateDefault();
        configureSettings?.Invoke(settings);

        var service = new AlertEvaluationService(
            connections, healthChecks, relationships, relationshipHealth, alertEvents, email, clock,
            new FakeSystemSettingsRepository(settings),
            NullLogger<AlertEvaluationService>.Instance);

        return (service, connections, healthChecks, relationships, relationshipHealth, alertEvents, email, clock);
    }

    [Fact]
    public async Task RunOnceAsync_sends_instant_critical_alert_for_subscription_error()
    {
        var (service, connections, _, relationships, relationshipHealth, _, email, clock) = CreateService();

        var source = NewConnection("src");
        var target = NewConnection("tgt");
        await connections.AddAsync(source);
        await connections.AddAsync(target);

        var relationship = NewConfirmedRelationship(source.Id, target.Id);
        await relationships.AddAsync(relationship);

        await relationshipHealth.AddAsync(new CdcRelationshipHealth
        {
            Id = Guid.NewGuid(),
            RelationshipId = relationship.Id,
            CheckedAt = clock.UtcNow,
            SlotActive = true,
            SubscriptionState = SubscriptionState.Error
        });

        await service.RunOnceAsync();

        var alert = Assert.Single(email.Alerts);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
    }

    [Fact]
    public async Task RunOnceAsync_does_not_resend_while_condition_stays_active_anti_flap()
    {
        var (service, connections, _, relationships, relationshipHealth, _, email, clock) = CreateService();

        var source = NewConnection("src");
        var target = NewConnection("tgt");
        await connections.AddAsync(source);
        await connections.AddAsync(target);

        var relationship = NewConfirmedRelationship(source.Id, target.Id);
        await relationships.AddAsync(relationship);

        await relationshipHealth.AddAsync(new CdcRelationshipHealth
        {
            Id = Guid.NewGuid(),
            RelationshipId = relationship.Id,
            CheckedAt = clock.UtcNow,
            SlotActive = true,
            SubscriptionState = SubscriptionState.Error
        });

        await service.RunOnceAsync();
        await service.RunOnceAsync();
        await service.RunOnceAsync();

        Assert.Single(email.Alerts);
    }

    [Fact]
    public async Task RunOnceAsync_resolves_alert_and_can_refire_after_condition_clears_and_returns()
    {
        var (service, connections, _, relationships, relationshipHealth, alertEvents, email, clock) = CreateService();

        var source = NewConnection("src");
        var target = NewConnection("tgt");
        await connections.AddAsync(source);
        await connections.AddAsync(target);

        var relationship = NewConfirmedRelationship(source.Id, target.Id);
        await relationships.AddAsync(relationship);

        await relationshipHealth.AddAsync(new CdcRelationshipHealth
        {
            Id = Guid.NewGuid(),
            RelationshipId = relationship.Id,
            CheckedAt = clock.UtcNow,
            SlotActive = true,
            SubscriptionState = SubscriptionState.Error
        });
        await service.RunOnceAsync();
        Assert.Single(email.Alerts);

        // Durum düzeliyor
        clock.UtcNow = clock.UtcNow.AddSeconds(30);
        await relationshipHealth.AddAsync(new CdcRelationshipHealth
        {
            Id = Guid.NewGuid(),
            RelationshipId = relationship.Id,
            CheckedAt = clock.UtcNow,
            SlotActive = true,
            SubscriptionState = SubscriptionState.Enabled
        });
        await service.RunOnceAsync();

        var resolved = await alertEvents.GetActiveAsync(AlertType.SubscriptionError, null, relationship.Id, default);
        Assert.Null(resolved);

        // Tekrar bozuluyor -> yeni bir alarm oluşup tekrar bildirim gönderilmeli
        clock.UtcNow = clock.UtcNow.AddSeconds(30);
        await relationshipHealth.AddAsync(new CdcRelationshipHealth
        {
            Id = Guid.NewGuid(),
            RelationshipId = relationship.Id,
            CheckedAt = clock.UtcNow,
            SlotActive = true,
            SubscriptionState = SubscriptionState.Error
        });
        await service.RunOnceAsync();

        Assert.Equal(2, email.Alerts.Count);
    }

    [Fact]
    public async Task RunOnceAsync_waits_for_sustained_duration_before_notifying_slot_inactive()
    {
        var (service, connections, _, relationships, relationshipHealth, _, email, clock) = CreateService(s => s.SlotInactiveMinutes = 5);

        var source = NewConnection("src");
        var target = NewConnection("tgt");
        await connections.AddAsync(source);
        await connections.AddAsync(target);

        var relationship = NewConfirmedRelationship(source.Id, target.Id);
        await relationships.AddAsync(relationship);

        await relationshipHealth.AddAsync(new CdcRelationshipHealth
        {
            Id = Guid.NewGuid(),
            RelationshipId = relationship.Id,
            CheckedAt = clock.UtcNow,
            SlotActive = false,
            SubscriptionState = SubscriptionState.Enabled
        });

        await service.RunOnceAsync();
        Assert.Empty(email.Alerts);

        clock.UtcNow = clock.UtcNow.AddMinutes(6);
        await service.RunOnceAsync();

        var alert = Assert.Single(email.Alerts);
        Assert.Equal(AlertSeverity.Critical, alert.Severity);
    }

    [Fact]
    public async Task RunOnceAsync_triggers_health_check_failed_only_after_consecutive_failures()
    {
        var (service, connections, healthChecks, _, _, _, email, clock) = CreateService(s => s.ConsecutiveHealthCheckFailures = 2);

        var connection = NewConnection("flaky");
        await connections.AddAsync(connection);

        await healthChecks.AddAsync(new ConnectionHealthCheck
        {
            Id = Guid.NewGuid(),
            ConnectionId = connection.Id,
            CheckedAt = clock.UtcNow,
            IsUp = false
        });
        await service.RunOnceAsync();
        Assert.Empty(email.Alerts);

        await healthChecks.AddAsync(new ConnectionHealthCheck
        {
            Id = Guid.NewGuid(),
            ConnectionId = connection.Id,
            CheckedAt = clock.UtcNow,
            IsUp = false
        });
        await service.RunOnceAsync();

        var alert = Assert.Single(email.Alerts);
        Assert.Equal(AlertSeverity.Warning, alert.Severity);
    }

    [Fact]
    public async Task RunOnceAsync_ignores_relationships_that_are_not_confirmed_or_manual()
    {
        var (service, connections, _, relationships, relationshipHealth, _, email, clock) = CreateService();

        var source = NewConnection("src");
        var target = NewConnection("tgt");
        await connections.AddAsync(source);
        await connections.AddAsync(target);

        var relationship = NewConfirmedRelationship(source.Id, target.Id);
        relationship.Status = CdcRelationshipStatus.Inferred;
        await relationships.AddAsync(relationship);

        await relationshipHealth.AddAsync(new CdcRelationshipHealth
        {
            Id = Guid.NewGuid(),
            RelationshipId = relationship.Id,
            CheckedAt = clock.UtcNow,
            SlotActive = true,
            SubscriptionState = SubscriptionState.Error
        });

        await service.RunOnceAsync();

        Assert.Empty(email.Alerts);
    }
}
