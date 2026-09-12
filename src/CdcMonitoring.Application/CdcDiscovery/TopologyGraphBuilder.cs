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
        var items = group
            .Select(r => new TopologyEdgeRelationship(
                r.PublicationName,
                r.SubscriptionName,
                r.SlotName,
                StatusText(r.Status),
                HealthText(r.LatestHealth),
                r.LatestHealth?.LagBytes,
                RelationshipColor(r)))
            .ToList();

        var worstColor = items
            .OrderBy(i => ColorSeverity.GetValueOrDefault(i.Color, 5))
            .First()
            .Color;

        var count = items.Count;
        var label = count == 1 ? SingleEdgeLabel(items[0]) : $"{count} ilişki";
        var title = count == 1
            ? SingleEdgeTitle(items[0])
            : $"{count} ilişki — detay için tıklayın";

        return new TopologyEdge(
            Id: $"{group.Key.SourceConnectionId}__{group.Key.TargetConnectionId}",
            From: group.Key.SourceConnectionId,
            To: group.Key.TargetConnectionId,
            Label: label,
            Title: title,
            Count: count,
            Width: EdgeWidth(count),
            Color: worstColor,
            Relationships: items);
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
