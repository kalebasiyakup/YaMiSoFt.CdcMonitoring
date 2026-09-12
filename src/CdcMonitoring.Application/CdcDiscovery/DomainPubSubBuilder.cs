using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.CdcDiscovery;

public enum CdcHealthLevel
{
    Healthy,
    Warning,
    Critical,
    Disabled,
    Unknown
}

public record DomainPubSubItem(
    Guid CounterpartConnectionId,
    string CounterpartName,
    string PublicationName,
    string SubscriptionName,
    string SlotName,
    string StatusText,
    string HealthText,
    long? LagBytes,
    CdcHealthLevel Health);

public record DomainPubSubView(
    Guid ConnectionId,
    string ConnectionName,
    IReadOnlyList<DomainPubSubItem> Published,
    IReadOnlyList<DomainPubSubItem> Subscribed);

/// <summary>
/// CDC ilişkilerini bağlantı (domain) başına "yayınladıkları" ve "abone oldukları" olarak ayırır.
/// Topoloji grafiği "hangi servis kime bağlı" sorusunu özetlerken, bu görünüm her
/// publication/subscription çiftini ayrı bir satırda okunaklı biçimde listelemek için kullanılır.
/// </summary>
public static class DomainPubSubBuilder
{
    public static IReadOnlyList<DomainPubSubView> Build(IEnumerable<CdcRelationshipSummary> relationships)
    {
        var active = relationships
            .Where(r => r.Status != CdcRelationshipStatus.Rejected)
            .ToList();

        return active
            .SelectMany(r => new[]
            {
                (Id: r.SourceConnectionId, Name: r.SourceConnectionName),
                (Id: r.TargetConnectionId, Name: r.TargetConnectionName)
            })
            .DistinctBy(c => c.Id)
            .OrderBy(c => c.Name)
            .Select(c => new DomainPubSubView(
                c.Id,
                c.Name,
                Sorted(active.Where(r => r.SourceConnectionId == c.Id)
                    .Select(r => ToItem(r, r.TargetConnectionId, r.TargetConnectionName))),
                Sorted(active.Where(r => r.TargetConnectionId == c.Id)
                    .Select(r => ToItem(r, r.SourceConnectionId, r.SourceConnectionName)))))
            .ToList();
    }

    // Sorunlu ilişkiler listenin başında görünsün; gerisi karşı taraf ve slot adına göre sabit sırada.
    private static IReadOnlyList<DomainPubSubItem> Sorted(IEnumerable<DomainPubSubItem> items) => items
        .OrderBy(i => Severity(i.Health))
        .ThenBy(i => i.CounterpartName)
        .ThenBy(i => i.SlotName)
        .ToList();

    private static int Severity(CdcHealthLevel level) => level switch
    {
        CdcHealthLevel.Critical => 0,
        CdcHealthLevel.Warning => 1,
        CdcHealthLevel.Disabled => 2,
        CdcHealthLevel.Unknown => 3,
        _ => 4
    };

    private static DomainPubSubItem ToItem(CdcRelationshipSummary r, Guid counterpartId, string counterpartName) => new(
        counterpartId,
        counterpartName,
        r.PublicationName,
        r.SubscriptionName,
        r.SlotName,
        StatusText(r.Status),
        HealthText(r.LatestHealth),
        r.LatestHealth?.LagBytes,
        HealthLevel(r.LatestHealth));

    private static CdcHealthLevel HealthLevel(CdcRelationshipHealthSummary? health) => health switch
    {
        null => CdcHealthLevel.Unknown,
        { SubscriptionState: SubscriptionState.Error } => CdcHealthLevel.Critical,
        { SubscriptionState: SubscriptionState.Disabled } => CdcHealthLevel.Disabled,
        { SubscriptionState: SubscriptionState.Enabled, SlotActive: true } => CdcHealthLevel.Healthy,
        _ => CdcHealthLevel.Warning
    };

    private static string StatusText(CdcRelationshipStatus status) => status switch
    {
        CdcRelationshipStatus.Inferred => "Keşfedildi (onay bekliyor)",
        CdcRelationshipStatus.Confirmed => "Onaylandı",
        CdcRelationshipStatus.Manual => "Manuel tanımlandı",
        _ => status.ToString()
    };

    private static string HealthText(CdcRelationshipHealthSummary? health)
    {
        if (health is null) return "Sağlık verisi yok";

        var state = health.SubscriptionState switch
        {
            SubscriptionState.Enabled => "Etkin",
            SubscriptionState.Disabled => "Devre dışı",
            SubscriptionState.Error => "Hata",
            _ => "Bilinmiyor"
        };

        return $"{state}, slot {(health.SlotActive ? "aktif" : "pasif")}";
    }
}
