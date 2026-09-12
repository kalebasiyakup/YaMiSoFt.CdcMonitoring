using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Domain.Enums;
using Xunit;

namespace CdcMonitoring.UnitTests.Connections;

public class DomainPubSubBuilderTests
{
    private static readonly Guid Merchant = Guid.NewGuid();
    private static readonly Guid Reimbursement = Guid.NewGuid();
    private static readonly Guid Transaction = Guid.NewGuid();

    private static CdcRelationshipSummary Relationship(
        Guid source,
        string sourceName,
        Guid target,
        string targetName,
        string slot,
        CdcRelationshipStatus status = CdcRelationshipStatus.Inferred,
        CdcRelationshipHealthSummary? health = null) => new(
            Guid.NewGuid(),
            source,
            sourceName,
            target,
            targetName,
            $"{slot}_pub",
            $"{slot}_sub",
            slot,
            status,
            ConfirmedBy: null,
            ConfirmedAt: null,
            CreatedAt: DateTimeOffset.Parse("2026-09-12T10:00:00Z"),
            health);

    private static CdcRelationshipHealthSummary Health(SubscriptionState state, bool slotActive, long? lag = null) => new(
        DateTimeOffset.Parse("2026-09-12T10:00:00Z"),
        slotActive,
        WalStatus: null,
        lag,
        state,
        LastSyncAt: null);

    private static CdcRelationshipSummary MerchantToReimbursement(string slot, CdcRelationshipHealthSummary? health = null) =>
        Relationship(Merchant, "dom-merchant-api", Reimbursement, "dom-reimbursement-api", slot, health: health);

    [Fact]
    public void Splits_each_connection_into_published_and_subscribed_lists()
    {
        var relationships = new[]
        {
            MerchantToReimbursement("dom_merchant_iban"),
            MerchantToReimbursement("dom_merchant_terminal"),
            Relationship(Transaction, "dom-transaction-api", Reimbursement, "dom-reimbursement-api", "dom_transaction_config"),
        };

        var views = DomainPubSubBuilder.Build(relationships);

        var merchant = Assert.Single(views, v => v.ConnectionId == Merchant);
        Assert.Equal(2, merchant.Published.Count);
        Assert.Empty(merchant.Subscribed);

        var reimbursement = Assert.Single(views, v => v.ConnectionId == Reimbursement);
        Assert.Empty(reimbursement.Published);
        Assert.Equal(3, reimbursement.Subscribed.Count);
    }

    [Fact]
    public void Every_relationship_stays_its_own_row_instead_of_being_aggregated()
    {
        var relationships = Enumerable.Range(0, 7)
            .Select(i => MerchantToReimbursement($"slot{i}"))
            .ToList();

        var views = DomainPubSubBuilder.Build(relationships);

        var merchant = Assert.Single(views, v => v.ConnectionId == Merchant);
        Assert.Equal(7, merchant.Published.Count);
        Assert.Equal(7, merchant.Published.Select(i => i.SlotName).Distinct().Count());
    }

    [Fact]
    public void Item_carries_the_counterpart_connection_for_both_directions()
    {
        var relationships = new[] { MerchantToReimbursement("dom_merchant_iban") };

        var views = DomainPubSubBuilder.Build(relationships);

        var published = Assert.Single(Assert.Single(views, v => v.ConnectionId == Merchant).Published);
        Assert.Equal(Reimbursement, published.CounterpartConnectionId);
        Assert.Equal("dom-reimbursement-api", published.CounterpartName);

        var subscribed = Assert.Single(Assert.Single(views, v => v.ConnectionId == Reimbursement).Subscribed);
        Assert.Equal(Merchant, subscribed.CounterpartConnectionId);
        Assert.Equal("dom-merchant-api", subscribed.CounterpartName);
    }

    [Theory]
    [InlineData(SubscriptionState.Enabled, true, CdcHealthLevel.Healthy)]
    [InlineData(SubscriptionState.Enabled, false, CdcHealthLevel.Warning)]
    [InlineData(SubscriptionState.Error, false, CdcHealthLevel.Critical)]
    [InlineData(SubscriptionState.Disabled, false, CdcHealthLevel.Disabled)]
    public void Maps_subscription_state_to_a_health_level(SubscriptionState state, bool slotActive, CdcHealthLevel expected)
    {
        var relationships = new[] { MerchantToReimbursement("dom_merchant_iban", Health(state, slotActive)) };

        var views = DomainPubSubBuilder.Build(relationships);

        var item = Assert.Single(Assert.Single(views, v => v.ConnectionId == Merchant).Published);
        Assert.Equal(expected, item.Health);
    }

    [Fact]
    public void Missing_health_data_is_unknown()
    {
        var relationships = new[] { MerchantToReimbursement("dom_merchant_iban") };

        var views = DomainPubSubBuilder.Build(relationships);

        var item = Assert.Single(Assert.Single(views, v => v.ConnectionId == Merchant).Published);
        Assert.Equal(CdcHealthLevel.Unknown, item.Health);
        Assert.Equal("Sağlık verisi yok", item.HealthText);
        Assert.Null(item.LagBytes);
    }

    [Fact]
    public void Unhealthy_rows_are_listed_first()
    {
        var relationships = new[]
        {
            MerchantToReimbursement("a_healthy", Health(SubscriptionState.Enabled, true)),
            MerchantToReimbursement("b_critical", Health(SubscriptionState.Error, false)),
            MerchantToReimbursement("c_warning", Health(SubscriptionState.Enabled, false)),
        };

        var views = DomainPubSubBuilder.Build(relationships);

        var published = Assert.Single(views, v => v.ConnectionId == Merchant).Published;
        Assert.Equal(["b_critical", "c_warning", "a_healthy"], published.Select(i => i.SlotName));
    }

    [Fact]
    public void Excludes_rejected_relationships()
    {
        var relationships = new[]
        {
            MerchantToReimbursement("dom_merchant_iban"),
            Relationship(Merchant, "dom-merchant-api", Reimbursement, "dom-reimbursement-api",
                "dom_merchant_terminal", CdcRelationshipStatus.Rejected),
        };

        var views = DomainPubSubBuilder.Build(relationships);

        var merchant = Assert.Single(views, v => v.ConnectionId == Merchant);
        Assert.Single(merchant.Published);
    }

    [Fact]
    public void Connection_that_only_appears_in_rejected_relationships_is_not_listed()
    {
        var relationships = new[]
        {
            Relationship(Merchant, "dom-merchant-api", Transaction, "dom-transaction-api",
                "dom_merchant_dealer", CdcRelationshipStatus.Rejected),
            MerchantToReimbursement("dom_merchant_iban"),
        };

        var views = DomainPubSubBuilder.Build(relationships);

        Assert.DoesNotContain(views, v => v.ConnectionId == Transaction);
    }

    [Fact]
    public void Views_are_ordered_by_connection_name()
    {
        var relationships = new[]
        {
            MerchantToReimbursement("dom_merchant_iban"),
            Relationship(Transaction, "dom-transaction-api", Reimbursement, "dom-reimbursement-api", "dom_transaction_config"),
        };

        var views = DomainPubSubBuilder.Build(relationships);

        Assert.Equal(["dom-merchant-api", "dom-reimbursement-api", "dom-transaction-api"],
            views.Select(v => v.ConnectionName));
    }
}
