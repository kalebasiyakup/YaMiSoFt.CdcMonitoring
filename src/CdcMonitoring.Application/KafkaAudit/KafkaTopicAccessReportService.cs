using CdcMonitoring.Application.Abstractions;

namespace CdcMonitoring.Application.KafkaAudit;

/// <summary>
/// Her topic için yazma/okuma ACL principal'larını ve o an aktif olarak abone olan
/// consumer group'ları tek raporda birleştirir (FR: Kafka Topic Erişim Raporu).
/// Internal topic'ler ("__" ile başlayan — __consumer_offsets, __transaction_state vb.)
/// Strimzi/Kafka'nın kendi iç işleyişine ait olduğundan rapora dahil edilmez.
/// </summary>
public class KafkaTopicAccessReportService(IKafkaAdminGateway gateway, IClock clock)
{
    private const string InternalTopicPrefix = "__";

    public async Task<KafkaTopicAccessReport> BuildReportAsync(CancellationToken ct = default)
    {
        var warnings = new List<string>();

        var topics = (await gateway.GetTopicsAsync(ct))
            .Where(t => !t.Name.StartsWith(InternalTopicPrefix, StringComparison.Ordinal))
            .OrderBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var aclOutcome = await gateway.GetAclEntriesAsync(ct);
        if (!aclOutcome.Succeeded)
            warnings.Add($"ACL bilgisi alınamadı (authorizer kapalı veya yetki yetersiz olabilir): {aclOutcome.Error}");

        var groupOutcome = await gateway.GetActiveConsumerGroupSubscriptionsAsync(ct);
        if (!groupOutcome.Succeeded)
            warnings.Add($"Consumer group bilgisi alınamadı: {groupOutcome.Error}");

        // Rapor yalnızca "izin verilmiş" (Allow) erişimi gösterir — Deny girdileri (ve tanınmayan
        // işlemler) hem özet tablodan hem detaylı ACL listesinden dışlanır.
        var allowedEntries = aclOutcome.Entries
            .Where(e => string.Equals(e.PermissionType, "Allow", StringComparison.Ordinal))
            .ToList();

        var topicEntries = allowedEntries.Where(e => e.ResourceType == "Topic").ToList();
        var activeGroupsByTopic = GroupActiveConsumerGroupsByTopic(groupOutcome.Groups);

        var rows = topics
            .Select(t => new KafkaTopicAccessReportRow(
                t.Name,
                t.PartitionCount,
                t.ReplicationFactor,
                PrincipalsFor(topicEntries, t.Name, IsWriteOperation),
                PrincipalsFor(topicEntries, t.Name, IsReadOperation),
                activeGroupsByTopic.GetValueOrDefault(t.Name, [])))
            .ToList();

        var aclEntries = allowedEntries
            .OrderBy(e => e.ResourceType, StringComparer.Ordinal)
            .ThenBy(e => e.ResourceName, StringComparer.Ordinal)
            .ThenBy(e => e.Principal, StringComparer.Ordinal)
            .ThenBy(e => e.Operation, StringComparer.Ordinal)
            .ToList();

        return new KafkaTopicAccessReport(rows, aclEntries, warnings, clock.UtcNow);
    }

    // ALL, hem yazma hem okumayı zımnen kapsadığından iki ayrı gruba (Write + Read) dahil edilir;
    // aksi halde ALL yetkisi olan bir principal özet tabloda yalnızca tek sütunda görünürdü.
    private static bool IsWriteOperation(string operation) => operation is "Write" or "All" or "IdempotentWrite";

    private static bool IsReadOperation(string operation) => operation is "Read" or "All";

    // ACL'in kaynak adı bir topic adı olmak zorunda değil: Aiven gibi yönetilen servislerde izinler
    // genellikle "*" (tüm topic'ler) veya önek (Prefixed) kalıbıyla tanımlanır — birebir ad
    // eşlemesi yapılırsa bu ACL'ler hiçbir topic'e bağlanmaz ve topoloji bağlantısız kalır.
    private static IReadOnlyList<string> PrincipalsFor(
        IReadOnlyList<KafkaAclEntry> topicEntries, string topicName, Func<string, bool> operationMatches) =>
        topicEntries
            .Where(e => operationMatches(e.Operation) && ResourceMatchesTopic(e, topicName))
            .Select(e => e.Principal)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(p => p, StringComparer.Ordinal)
            .ToList();

    // Aiven kaynak adlarını da kaçışlı döner ("event-1" → "event\-1") — karşılaştırmadan önce çözülür.
    private static bool ResourceMatchesTopic(KafkaAclEntry entry, string topicName)
    {
        var resourceName = KafkaPrincipalFormatter.Unescape(entry.ResourceName);

        return string.Equals(entry.PatternType, "Prefixed", StringComparison.Ordinal)
            ? topicName.StartsWith(resourceName, StringComparison.Ordinal)
            : resourceName == "*" || string.Equals(resourceName, topicName, StringComparison.Ordinal);
    }

    // "Aktif" = state Stable VE en az bir topic'e gerçekten atanmış partition'ı olan grup;
    // Stable ama boş assignment'lı (ör. tam rebalance ortasında yakalanmış) gruplar dahil edilmez.
    private static Dictionary<string, IReadOnlyList<string>> GroupActiveConsumerGroupsByTopic(
        IReadOnlyList<KafkaConsumerGroupSubscription> groups)
    {
        var byTopic = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var group in groups)
        {
            foreach (var topicName in group.SubscribedTopics.Distinct(StringComparer.Ordinal))
            {
                if (!byTopic.TryGetValue(topicName, out var list))
                    byTopic[topicName] = list = [];

                list.Add(group.GroupId);
            }
        }

        return byTopic.ToDictionary(
            kv => kv.Key,
            IReadOnlyList<string> (kv) => kv.Value.OrderBy(g => g, StringComparer.Ordinal).ToList(),
            StringComparer.Ordinal);
    }
}
