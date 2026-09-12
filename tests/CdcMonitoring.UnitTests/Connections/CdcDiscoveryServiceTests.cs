using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using CdcMonitoring.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CdcMonitoring.UnitTests.Connections;

public class CdcDiscoveryServiceTests
{
    private static PgConnection NewConnection(string name, string host, string db) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Host = host,
        Port = 5432,
        DatabaseName = db,
        Username = "monitor_ro",
        EncryptedPassword = "enc:s3cr3t!",
        EnvironmentTag = "Prod-DC1",
        IsActive = true,
        CreatedAt = DateTimeOffset.UtcNow,
        CreatedBy = "system"
    };

    private static (CdcDiscoveryService Service, InMemoryPgConnectionRepository Connections, InMemoryCdcRelationshipRepository Relationships,
        InMemoryCdcRelationshipHealthRepository Health, FakePostgresInspector Inspector, RecordingMetricsRecorder Metrics) CreateService()
    {
        var connections = new InMemoryPgConnectionRepository();
        var relationships = new InMemoryCdcRelationshipRepository();
        var health = new InMemoryCdcRelationshipHealthRepository();
        var inspector = new FakePostgresInspector();
        var metrics = new RecordingMetricsRecorder();

        var settings = FakeSystemSettingsRepository.CreateDefault();
        settings.DiscoveryMaxDegreeOfParallelism = 2;
        settings.DiscoveryTimeoutSeconds = 5;

        var service = new CdcDiscoveryService(
            connections, relationships, health, new FakePasswordProtector(), inspector, metrics,
            new FixedClock(DateTimeOffset.UtcNow),
            new FakeSystemSettingsRepository(settings),
            NullLogger<CdcDiscoveryService>.Instance);

        return (service, connections, relationships, health, inspector, metrics);
    }

    [Fact]
    public async Task RunOnceAsync_creates_inferred_relationship_when_conninfo_matches_registered_source()
    {
        var (service, connections, relationships, health, inspector, metrics) = CreateService();

        var source = NewConnection("orders-source", "db1.internal", "orders");
        var target = NewConnection("orders-replica", "db2.internal", "orders_replica");
        await connections.AddAsync(source);
        await connections.AddAsync(target);

        inspector.Subscriptions[target.Id] =
        [
            new SubscriptionInfo("sub_orders", true, "slot_orders", "host=db1.internal port=5432 dbname=orders user=repl", ["pub_orders"])
        ];
        inspector.Slots[source.Id] =
        [
            new ReplicationSlotInfo("slot_orders", true, "reserved", 1024, "orders")
        ];
        inspector.SubscriptionStats[target.Id] =
        [
            new SubscriptionStatInfo("sub_orders", true, DateTimeOffset.UtcNow)
        ];

        await service.RunOnceAsync();

        var all = await relationships.GetAllAsync();
        var relationship = Assert.Single(all);
        Assert.Equal(source.Id, relationship.SourceConnectionId);
        Assert.Equal(target.Id, relationship.TargetConnectionId);
        Assert.Equal("slot_orders", relationship.SlotName);
        Assert.Equal(CdcRelationshipStatus.Inferred, relationship.Status);

        var healthEntry = Assert.Single(health.Entries);
        Assert.True(healthEntry.SlotActive);
        Assert.Equal(1024, healthEntry.LagBytes);
        Assert.Equal(SubscriptionState.Enabled, healthEntry.SubscriptionState);

        Assert.Single(metrics.CdcHealthRecords);
    }

    [Fact]
    public async Task RunOnceAsync_skips_subscription_when_source_not_registered()
    {
        var (service, connections, relationships, _, inspector, _) = CreateService();

        var target = NewConnection("orders-replica", "db2.internal", "orders_replica");
        await connections.AddAsync(target);

        inspector.Subscriptions[target.Id] =
        [
            new SubscriptionInfo("sub_orders", true, "slot_orders", "host=unregistered.internal port=5432 dbname=orders user=repl", ["pub_orders"])
        ];

        await service.RunOnceAsync();

        Assert.Empty(await relationships.GetAllAsync());
    }

    [Fact]
    public async Task RunOnceAsync_does_not_downgrade_a_confirmed_relationship_status()
    {
        var (service, connections, relationships, _, inspector, _) = CreateService();

        var source = NewConnection("orders-source", "db1.internal", "orders");
        var target = NewConnection("orders-replica", "db2.internal", "orders_replica");
        await connections.AddAsync(source);
        await connections.AddAsync(target);

        await relationships.AddAsync(new CdcRelationship
        {
            Id = Guid.NewGuid(),
            SourceConnectionId = source.Id,
            TargetConnectionId = target.Id,
            PublicationName = "pub_orders",
            SubscriptionName = "sub_orders",
            SlotName = "slot_orders",
            Status = CdcRelationshipStatus.Confirmed,
            ConfirmedBy = "yakup.kalebasi",
            ConfirmedAt = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        });

        inspector.Subscriptions[target.Id] =
        [
            new SubscriptionInfo("sub_orders", true, "slot_orders", "host=db1.internal port=5432 dbname=orders user=repl", ["pub_orders"])
        ];

        await service.RunOnceAsync();

        var relationship = Assert.Single(await relationships.GetAllAsync());
        Assert.Equal(CdcRelationshipStatus.Confirmed, relationship.Status);
    }
}
