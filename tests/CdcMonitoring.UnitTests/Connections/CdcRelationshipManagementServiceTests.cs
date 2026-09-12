using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using CdcMonitoring.UnitTests.TestDoubles;
using Xunit;

namespace CdcMonitoring.UnitTests.Connections;

public class CdcRelationshipManagementServiceTests
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

    private static (CdcRelationshipManagementService Service, InMemoryCdcRelationshipRepository Relationships,
        InMemoryPgConnectionRepository Connections, InMemoryAuditLogRepository AuditLog) CreateService()
    {
        var relationships = new InMemoryCdcRelationshipRepository();
        var health = new InMemoryCdcRelationshipHealthRepository();
        var connections = new InMemoryPgConnectionRepository();
        var auditLog = new InMemoryAuditLogRepository();

        var service = new CdcRelationshipManagementService(
            relationships, health, connections, auditLog,
            new FixedCurrentUserAccessor("yakup.kalebasi"),
            new FixedClock(DateTimeOffset.Parse("2026-09-12T10:00:00Z")));

        return (service, relationships, connections, auditLog);
    }

    [Fact]
    public async Task ConfirmAsync_marks_relationship_confirmed_and_writes_audit_log()
    {
        var (service, relationships, _, auditLog) = CreateService();
        var relationship = new CdcRelationship
        {
            Id = Guid.NewGuid(),
            SourceConnectionId = Guid.NewGuid(),
            TargetConnectionId = Guid.NewGuid(),
            PublicationName = "pub_orders",
            SubscriptionName = "sub_orders",
            SlotName = "slot_orders",
            Status = CdcRelationshipStatus.Inferred,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await relationships.AddAsync(relationship);

        await service.ConfirmAsync(relationship.Id);

        var updated = await relationships.GetByIdAsync(relationship.Id);
        Assert.Equal(CdcRelationshipStatus.Confirmed, updated!.Status);
        Assert.Equal("yakup.kalebasi", updated.ConfirmedBy);
        Assert.Contains(auditLog.Entries, e => e.Action == AuditAction.Confirmed);
    }

    [Fact]
    public async Task RejectAsync_marks_relationship_rejected()
    {
        var (service, relationships, _, _) = CreateService();
        var relationship = new CdcRelationship
        {
            Id = Guid.NewGuid(),
            SourceConnectionId = Guid.NewGuid(),
            TargetConnectionId = Guid.NewGuid(),
            PublicationName = "pub_orders",
            SubscriptionName = "sub_orders",
            SlotName = "slot_orders",
            Status = CdcRelationshipStatus.Inferred,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await relationships.AddAsync(relationship);

        await service.RejectAsync(relationship.Id);

        var updated = await relationships.GetByIdAsync(relationship.Id);
        Assert.Equal(CdcRelationshipStatus.Rejected, updated!.Status);
    }

    [Fact]
    public async Task CreateManualAsync_creates_relationship_with_manual_status()
    {
        var (service, _, connections, auditLog) = CreateService();
        var source = NewConnection("src");
        var target = NewConnection("tgt");
        await connections.AddAsync(source);
        await connections.AddAsync(target);

        var summary = await service.CreateManualAsync(new ManualRelationshipRequest(
            source.Id, target.Id, "pub_orders", "sub_orders", "slot_orders"));

        Assert.Equal(CdcRelationshipStatus.Manual, summary.Status);
        Assert.Equal("yakup.kalebasi", summary.ConfirmedBy);
        Assert.Contains(auditLog.Entries, e => e.Action == AuditAction.Created && e.EntityType == nameof(CdcRelationship));
    }

    [Fact]
    public async Task CreateManualAsync_rejects_source_equal_to_target()
    {
        var (service, _, connections, _) = CreateService();
        var conn = NewConnection("only");
        await connections.AddAsync(conn);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateManualAsync(
            new ManualRelationshipRequest(conn.Id, conn.Id, "pub", "sub", "slot")));
    }

    [Fact]
    public async Task CreateManualAsync_rejects_duplicate_relationship()
    {
        var (service, _, connections, _) = CreateService();
        var source = NewConnection("src");
        var target = NewConnection("tgt");
        await connections.AddAsync(source);
        await connections.AddAsync(target);

        await service.CreateManualAsync(new ManualRelationshipRequest(source.Id, target.Id, "pub", "sub", "slot"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateManualAsync(
            new ManualRelationshipRequest(source.Id, target.Id, "pub", "sub", "slot")));
    }
}
