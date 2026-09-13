using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.CdcDiscovery;

public record TopologyEdgeRelationship(
    string PublicationName,
    string SubscriptionName,
    string SlotName,
    string StatusText,
    string HealthText,
    long? LagBytes,
    string Color);

public record TopologyEdge(
    string Id,
    Guid From,
    Guid To,
    string Label,
    string Title,
    int Count,
    double Width,
    string Color,
    IReadOnlyList<TopologyEdgeRelationship> Relationships);

/// <summary>
/// Kaynak → publication (hub) → hedef şeklinde üç katmanlı, akış yönlü topoloji grafiği.
/// Bir publication'a birden fazla subscription bağlanabildiğinde (fan-out), hub tek düğüm
/// olarak kalır ve birden çok leaf edge'e ayrılır.
/// </summary>
public record TopologyHierarchyNode(string Id, string Label, string Kind); // Kind: "source" | "hub" | "target"

public record TopologyHubEdge(string Id, Guid FromSourceId, string ToHubId);

public record TopologyLeafEdge(
    string Id,
    string FromHubId,
    Guid ToTargetId,
    string Label,
    string Title,
    int Count,
    double Width,
    string Color,
    IReadOnlyList<TopologyEdgeRelationship> Relationships);

public record TopologyHierarchy(
    IReadOnlyList<TopologyHierarchyNode> Nodes,
    IReadOnlyList<TopologyHubEdge> HubEdges,
    IReadOnlyList<TopologyLeafEdge> LeafEdges);

/// <summary>
/// Aynı kaynak/hedef bağlantı çifti arasındaki birden çok CDC ilişkisini (publication/subscription/slot)
/// topoloji diyagramında tek bir kenarda özetler; ayrıntılar tıklanınca gösterilir.
/// </summary>
public static class TopologyGraphBuilder
{
    // Küçük değer = daha ciddi durum. Bir grup içindeki en kötü renk, kenarın rengini belirler.
    private static readonly Dictionary<string, int> ColorSeverity = new()
    {
        ["#dc3545"] = 0, // hata
        ["#ffc107"] = 1, // uyarı
        ["#6c757d"] = 2, // devre dışı
        ["#adb5bd"] = 3, // veri yok
        ["#28a745"] = 4  // sağlıklı
    };

    public static IReadOnlyList<TopologyEdge> BuildEdges(IEnumerable<CdcRelationshipSummary> relationships)
    {
        return relationships
            .Where(r => r.Status != CdcRelationshipStatus.Rejected)
            .GroupBy(r => (r.SourceConnectionId, r.TargetConnectionId))
            .Select(BuildEdge)
            .ToList();
    }

    private static TopologyEdge BuildEdge(IGrouping<(Guid SourceConnectionId, Guid TargetConnectionId), CdcRelationshipSummary> group)
    {
        var items = ToRelationshipItems(group);
        var summary = Summarize(items);

        return new TopologyEdge(
            Id: $"{group.Key.SourceConnectionId}__{group.Key.TargetConnectionId}",
            From: group.Key.SourceConnectionId,
            To: group.Key.TargetConnectionId,
            Label: summary.Label,
            Title: summary.Title,
            Count: summary.Count,
            Width: summary.Width,
            Color: summary.Color,
            Relationships: items);
    }

    /// <summary>
    /// Kaynak → publication (hub) → hedef üç katmanlı görünüm. Her (source, publication) çifti
    /// tek bir hub düğümü olur; aynı publication'dan birden çok subscription'a giden ilişkiler
    /// o hub'dan çıkan ayrı leaf edge'ler (fan-out) olarak kalır.
    /// </summary>
    public static TopologyHierarchy BuildHierarchy(IEnumerable<CdcRelationshipSummary> relationships)
    {
        var active = relationships.Where(r => r.Status != CdcRelationshipStatus.Rejected).ToList();

        var sourceNames = new Dictionary<Guid, string>();
        var targetNames = new Dictionary<Guid, string>();
        var hubEdges = new List<TopologyHubEdge>();
        var leafEdges = new List<TopologyLeafEdge>();

        var hubGroups = active.GroupBy(r => (r.SourceConnectionId, r.SourceConnectionName, r.PublicationName));

        foreach (var hubGroup in hubGroups)
        {
            var (sourceId, sourceName, publicationName) = hubGroup.Key;
            sourceNames[sourceId] = sourceName;

            var hubId = HubId(sourceId, publicationName);
            hubEdges.Add(new TopologyHubEdge(Id: $"{sourceId}__{hubId}", FromSourceId: sourceId, ToHubId: hubId));

            foreach (var leafGroup in hubGroup.GroupBy(r => (r.TargetConnectionId, r.TargetConnectionName)))
            {
                var (targetId, targetName) = leafGroup.Key;
                targetNames[targetId] = targetName;

                var items = ToRelationshipItems(leafGroup);
                var summary = Summarize(items);

                leafEdges.Add(new TopologyLeafEdge(
                    Id: $"{hubId}__{targetId}",
                    FromHubId: hubId,
                    ToTargetId: targetId,
                    Label: summary.Label,
                    Title: summary.Title,
                    Count: summary.Count,
                    Width: summary.Width,
                    Color: summary.Color,
                    Relationships: items));
            }
        }

        // Bir bağlantı hem kaynak (yayıncı) hem hedef (abone) rolündeyse basitlik için kaynak sayılır.
        foreach (var id in sourceNames.Keys)
            targetNames.Remove(id);

        var nodes = new List<TopologyHierarchyNode>();
        nodes.AddRange(sourceNames.Select(kv => new TopologyHierarchyNode(kv.Key.ToString(), kv.Value, "source")));
        nodes.AddRange(targetNames.Select(kv => new TopologyHierarchyNode(kv.Key.ToString(), kv.Value, "target")));
        nodes.AddRange(hubGroups.Select(g =>
            new TopologyHierarchyNode(HubId(g.Key.SourceConnectionId, g.Key.PublicationName), g.Key.PublicationName, "hub")));

        return new TopologyHierarchy(nodes, hubEdges, leafEdges);
    }

    private static string HubId(Guid sourceConnectionId, string publicationName) => $"pub::{sourceConnectionId}::{publicationName}";

    private static IReadOnlyList<TopologyEdgeRelationship> ToRelationshipItems(IEnumerable<CdcRelationshipSummary> group) => group
        .Select(r => new TopologyEdgeRelationship(
            r.PublicationName,
            r.SubscriptionName,
            r.SlotName,
            StatusText(r.Status),
            HealthText(r.LatestHealth),
            r.LatestHealth?.LagBytes,
            RelationshipColor(r)))
        .ToList();

    private static (string Label, string Title, int Count, double Width, string Color) Summarize(
        IReadOnlyList<TopologyEdgeRelationship> items)
    {
        var worstColor = items
            .OrderBy(i => ColorSeverity.GetValueOrDefault(i.Color, 5))
            .First()
            .Color;

        var count = items.Count;
        var label = count == 1 ? SingleEdgeLabel(items[0]) : $"{count} ilişki";
        var title = count == 1
            ? SingleEdgeTitle(items[0])
            : $"{count} ilişki — detay için tıklayın";

        return (label, title, count, EdgeWidth(count), worstColor);
    }

    private static double EdgeWidth(int count) => count <= 1 ? 1 : Math.Min(1 + (count - 1) * 0.5, 6);

    private static string SingleEdgeLabel(TopologyEdgeRelationship r) =>
        r.LagBytes is { } lag ? $"{r.SlotName}\n{lag:N0}B lag" : r.SlotName;

    private static string SingleEdgeTitle(TopologyEdgeRelationship r) =>
        string.Join("\n",
        [
            $"Publication: {r.PublicationName}",
            $"Subscription: {r.SubscriptionName}",
            $"Slot: {r.SlotName}",
            $"Kayıt durumu: {r.StatusText}",
            $"Sağlık: {r.HealthText}"
        ]);

    private static string StatusText(CdcRelationshipStatus status) => status switch
    {
        CdcRelationshipStatus.Inferred => "Keşfedildi (onay bekliyor)",
        CdcRelationshipStatus.Confirmed => "Onaylandı",
        CdcRelationshipStatus.Manual => "Manuel tanımlandı",
        _ => status.ToString()
    };

    private static string HealthText(CdcRelationshipHealthSummary? health)
    {
        if (health is null) return "Veri yok";
        var state = SubscriptionStateText(health.SubscriptionState);
        return $"{state}, slot {(health.SlotActive ? "aktif" : "pasif")}";
    }

    private static string SubscriptionStateText(SubscriptionState state) => state switch
    {
        SubscriptionState.Enabled => "Etkin",
        SubscriptionState.Disabled => "Devre dışı",
        SubscriptionState.Error => "Hata",
        _ => "Bilinmiyor"
    };

    private static string RelationshipColor(CdcRelationshipSummary r)
    {
        var health = r.LatestHealth;
        if (health is null) return "#adb5bd";

        return health.SubscriptionState switch
        {
            SubscriptionState.Enabled when health.SlotActive => "#28a745",
            SubscriptionState.Disabled => "#6c757d",
            SubscriptionState.Error => "#dc3545",
            _ => "#ffc107"
        };
    }
}
