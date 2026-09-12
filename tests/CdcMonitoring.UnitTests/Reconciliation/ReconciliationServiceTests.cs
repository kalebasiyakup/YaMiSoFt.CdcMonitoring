using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Application.Reconciliation;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;
using CdcMonitoring.UnitTests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CdcMonitoring.UnitTests.Reconciliation;

public class ReconciliationServiceTests
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

    private static (ReconciliationService Service, InMemoryCdcRelationshipRepository Relationships,
        InMemoryReconciliationResultRepository Results, FakePostgresInspector Inspector, FakeEmailNotifier Email) CreateService()
    {
        var relationships = new InMemoryCdcRelationshipRepository();
        var results = new InMemoryReconciliationResultRepository();
        var inspector = new FakePostgresInspector();
        var email = new FakeEmailNotifier();

        var service = new ReconciliationService(
            relationships, results, new FakePasswordProtector(), inspector, email,
            new FixedClock(DateTimeOffset.UtcNow),
            NullLogger<ReconciliationService>.Instance);

        return (service, relationships, results, inspector, email);
    }

    [Fact]
    public async Task RunOnceAsync_records_match_when_checksums_are_equal_and_sends_no_email()
    {
        var (service, relationships, results, inspector, email) = CreateService();

        var source = NewConnection("src");
        var target = NewConnection("tgt");
        var relationship = new CdcRelationship
        {
            Id = Guid.NewGuid(),
            SourceConnectionId = source.Id,
            SourceConnection = source,
            TargetConnectionId = target.Id,
            TargetConnection = target,
            PublicationName = "pub_orders",
            SubscriptionName = "sub_orders",
            SlotName = "slot_orders",
            Status = CdcRelationshipStatus.Confirmed,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await relationships.AddAsync(relationship);

        inspector.PublicationTables[source.Id] = [new PublicationTableInfo("public", "orders")];
        inspector.TableChecksums[(source.Id, "public", "orders")] = new TableChecksum(100, "abc123");
        inspector.TableChecksums[(target.Id, "public", "orders")] = new TableChecksum(100, "abc123");

        await service.RunOnceAsync();

        var result = Assert.Single(results.Results);
        Assert.True(result.IsMatch);
        Assert.Equal(100, result.SourceRowCount);
        Assert.Equal(100, result.TargetRowCount);
        Assert.Empty(email.Reports);
    }

    [Fact]
    public async Task RunOnceAsync_records_mismatch_and_sends_report_email_when_row_counts_differ()
    {
        var (service, relationships, results, inspector, email) = CreateService();

        var source = NewConnection("src");
        var target = NewConnection("tgt");
        var relationship = new CdcRelationship
        {
            Id = Guid.NewGuid(),
            SourceConnectionId = source.Id,
            SourceConnection = source,
            TargetConnectionId = target.Id,
            TargetConnection = target,
            PublicationName = "pub_orders",
            SubscriptionName = "sub_orders",
            SlotName = "slot_orders",
            Status = CdcRelationshipStatus.Confirmed,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await relationships.AddAsync(relationship);

        inspector.PublicationTables[source.Id] = [new PublicationTableInfo("public", "orders")];
        inspector.TableChecksums[(source.Id, "public", "orders")] = new TableChecksum(100, "abc123");
        inspector.TableChecksums[(target.Id, "public", "orders")] = new TableChecksum(97, "xyz789");

        await service.RunOnceAsync();

        var result = Assert.Single(results.Results);
        Assert.False(result.IsMatch);
        Assert.Contains("TUTARSIZ", result.Details);

        var report = Assert.Single(email.Reports);
        Assert.Contains("tutarsızlık", report.Subject, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RunOnceAsync_ignores_relationships_that_are_only_inferred()
    {
        var (service, relationships, results, inspector, _) = CreateService();

        var source = NewConnection("src");
        var target = NewConnection("tgt");
        var relationship = new CdcRelationship
        {
            Id = Guid.NewGuid(),
            SourceConnectionId = source.Id,
            SourceConnection = source,
            TargetConnectionId = target.Id,
            TargetConnection = target,
            PublicationName = "pub_orders",
            SubscriptionName = "sub_orders",
            SlotName = "slot_orders",
            Status = CdcRelationshipStatus.Inferred,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await relationships.AddAsync(relationship);

        await service.RunOnceAsync();

        Assert.Empty(results.Results);
    }
}
