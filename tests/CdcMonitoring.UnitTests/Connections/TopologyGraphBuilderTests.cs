using CdcMonitoring.Application.CdcDiscovery;
using CdcMonitoring.Domain.Enums;
using Xunit;

namespace CdcMonitoring.UnitTests.Connections;

public class TopologyGraphBuilderTests
{
    private static readonly Guid Merchant = Guid.NewGuid();
    private static readonly Guid Reimbursement = Guid.NewGuid();
    private static readonly Guid Other = Guid.NewGuid();

    private static CdcRelationshipSummary Relationship(
        Guid source,
        Guid target,
        string slot,
        CdcRelationshipStatus status = CdcRelationshipStatus.Inferred,
        CdcRelationshipHealthSummary? health = null) => new(
            Guid.NewGuid(),
            source,
            "kaynak",
            target,
            "hedef",
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

    [Fact]
    public void Groups_multiple_relationships_between_same_node_pair_into_one_edge()
    {
        var relationships = new[]
        {
            Relationship(Merchant, Reimbursement, "iban"),
            Relationship(Merchant, Reimbursement, "brand_line_item"),
            Relationship(Merchant, Reimbursement, "terminal"),
        };

        var edges = TopologyGraphBuilder.BuildEdges(relationships);

        var edge = Assert.Single(edges);
        Assert.Equal(Merchant, edge.From);
        Assert.Equal(Reimbursement, edge.To);
        Assert.Equal(3, edge.Count);
        Assert.Equal("3 ilişki", edge.Label);
        Assert.Equal(3, edge.Relationships.Count);
    }

    [Fact]
    public void Keeps_separate_edges_for_different_node_pairs()
    {
        var relationships = new[]
        {
            Relationship(Merchant, Reimbursement, "iban"),
            Relationship(Merchant, Other, "brand_line_item"),
        };

        var edges = TopologyGraphBuilder.BuildEdges(relationships);

        Assert.Equal(2, edges.Count);
        Assert.Contains(edges, e => e.From == Merchant && e.To == Reimbursement);
        Assert.Contains(edges, e => e.From == Merchant && e.To == Other);
    }

    [Fact]
    public void Excludes_rejected_relationships()
    {
        var relationships = new[]
        {
            Relationship(Merchant, Reimbursement, "iban"),
            Relationship(Merchant, Reimbursement, "brand_line_item", CdcRelationshipStatus.Rejected),
        };

        var edges = TopologyGraphBuilder.BuildEdges(relationships);

        var edge = Assert.Single(edges);
        Assert.Equal(1, edge.Count);
    }

    [Fact]
    public void Single_relationship_edge_keeps_slot_based_label_instead_of_count()
    {
        var relationships = new[]
        {
            Relationship(Merchant, Reimbursement, "iban", health: Health(SubscriptionState.Enabled, true, 512))
        };

        var edges = TopologyGraphBuilder.BuildEdges(relationships);

        var edge = Assert.Single(edges);
        Assert.Equal("iban\n512B lag", edge.Label);
    }

    [Fact]
    public void Aggregated_edge_color_reflects_the_worst_health_in_the_group()
    {
        var relationships = new[]
        {
            Relationship(Merchant, Reimbursement, "iban", health: Health(SubscriptionState.Enabled, true)),
            Relationship(Merchant, Reimbursement, "brand_line_item", health: Health(SubscriptionState.Error, false)),
            Relationship(Merchant, Reimbursement, "terminal", health: Health(SubscriptionState.Disabled, false)),
        };

        var edges = TopologyGraphBuilder.BuildEdges(relationships);

        var edge = Assert.Single(edges);
        Assert.Equal("#dc3545", edge.Color); // hata en ciddi durum, öne çıkmalı
    }

    [Fact]
    public void Missing_health_data_is_treated_as_unknown_not_worst_case()
    {
        var relationships = new[]
        {
            Relationship(Merchant, Reimbursement, "iban", health: null),
            Relationship(Merchant, Reimbursement, "brand_line_item", health: Health(SubscriptionState.Enabled, true)),
        };

        var edges = TopologyGraphBuilder.BuildEdges(relationships);

        var edge = Assert.Single(edges);
        Assert.Equal("#adb5bd", edge.Color);
    }

    [Fact]
    public void Edge_width_scales_with_relationship_count_but_is_capped()
    {
        var single = TopologyGraphBuilder.BuildEdges([Relationship(Merchant, Reimbursement, "iban")]);
        Assert.Equal(1, Assert.Single(single).Width);

        var many = Enumerable.Range(0, 20)
            .Select(i => Relationship(Merchant, Reimbursement, $"slot{i}"))
            .ToList();
        var manyEdges = TopologyGraphBuilder.BuildEdges(many);

        var edge = Assert.Single(manyEdges);
        Assert.True(edge.Width > 1);
        Assert.True(edge.Width <= 6);
    }

    [Fact]
    public void Edge_id_is_stable_for_the_same_node_pair()
    {
        var relationships = new[]
        {
            Relationship(Merchant, Reimbursement, "iban"),
            Relationship(Merchant, Reimbursement, "brand_line_item"),
        };

        var edges = TopologyGraphBuilder.BuildEdges(relationships);

        var edge = Assert.Single(edges);
        Assert.Equal($"{Merchant}__{Reimbursement}", edge.Id);
    }
}
