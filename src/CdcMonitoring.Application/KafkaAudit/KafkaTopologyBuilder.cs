namespace CdcMonitoring.Application.KafkaAudit;

public record KafkaTopologyNode(string Id, string Label, string Kind); // Kind: "producer" | "topic" | "consumer-group" | "reader" | "group-acl-principal"

public record KafkaTopologyEdge(string Id, string FromId, string ToId, string Kind); // Kind: "write" | "read" | "consumes" | "group-acl"

public record KafkaTopologyGraph(IReadOnlyList<KafkaTopologyNode> Nodes, IReadOnlyList<KafkaTopologyEdge> Edges);

/// <summary>
/// Erişim raporunu bir grafiğe çevirir: yazma yetkisi olan principal'lar → topic → (okuma
/// yetkisi olan principal'lar ve/veya o an abone olan consumer group'lar). Bir principal hem
/// yazıp hem okuyabildiğinden düğüm id'leri "producer::" / "reader::" önekiyle ayrıştırılır —
/// aynı principal iki farklı düğüm olarak (rolüne göre) görünebilir. Ayrıca, Group kaynak
/// tipindeki Allow ACL girdileri "hangi principal hangi consumer group'u kullanmaya yetkili"
/// bilgisini principal → group kenarı olarak ekler — bu, grubun o an fiilen aktif olup
/// olmadığından (topic → group "consumes" kenarı) bağımsızdır; yetkili ama henüz hiç
/// kullanılmamış gruplar da böylece grafikte görünür.
/// </summary>
public static class KafkaTopologyBuilder
{
    public static KafkaTopologyGraph Build(
        IReadOnlyList<KafkaTopicAccessReportRow> topics,
        IReadOnlyList<KafkaAclEntry>? aclEntries = null)
    {
        var nodes = new Dictionary<string, KafkaTopologyNode>(StringComparer.Ordinal);
        var edges = new List<KafkaTopologyEdge>();

        void AddNode(string id, string label, string kind)
        {
            if (!nodes.ContainsKey(id))
                nodes[id] = new KafkaTopologyNode(id, label, kind);
        }

        foreach (var topic in topics)
        {
            var topicId = $"topic::{topic.TopicName}";
            AddNode(topicId, topic.TopicName, "topic");

            foreach (var principal in topic.WritePrincipals)
            {
                var producerId = $"producer::{principal}";
                AddNode(producerId, KafkaPrincipalFormatter.DisplayName(principal), "producer");
                edges.Add(new KafkaTopologyEdge($"{producerId}__{topicId}", producerId, topicId, "write"));
            }

            foreach (var principal in topic.ReadPrincipals)
            {
                var readerId = $"reader::{principal}";
                AddNode(readerId, KafkaPrincipalFormatter.DisplayName(principal), "reader");
                edges.Add(new KafkaTopologyEdge($"{topicId}__{readerId}", topicId, readerId, "read"));
            }

            foreach (var groupId in topic.ActiveConsumerGroups)
            {
                var consumerGroupId = $"group::{groupId}";
                AddNode(consumerGroupId, groupId, "consumer-group");
                edges.Add(new KafkaTopologyEdge($"{topicId}__{consumerGroupId}", topicId, consumerGroupId, "consumes"));
            }
        }

        // Aynı principal, aynı group kaynağına birden fazla ACL girdisiyle (ör. Read + Describe
        // gibi ayrı işlemler için ayrı girdiler) yetkilendirilmiş olabilir — hepsi tek bir kenara
        // indirgenir, aksi halde aynı id'li iki kenar vis.js DataSet'inde çakışma hatası verir.
        var addedGroupAclEdgeIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in (aclEntries ?? [])
            .Where(e => e.ResourceType == "Group"
                        && string.Equals(e.PermissionType, "Allow", StringComparison.Ordinal)
                        && !IsWildcardLiteral(e)))
        {
            var groupName = KafkaPrincipalFormatter.Unescape(entry.ResourceName);
            var consumerGroupId = $"group::{groupName}";
            AddNode(consumerGroupId, groupName, "consumer-group");

            var principalId = $"group-acl::{entry.Principal}";
            AddNode(principalId, KafkaPrincipalFormatter.DisplayName(entry.Principal), "group-acl-principal");

            var edgeId = $"{principalId}__{consumerGroupId}";
            if (addedGroupAclEdgeIds.Add(edgeId))
                edges.Add(new KafkaTopologyEdge(edgeId, principalId, consumerGroupId, "group-acl"));
        }

        return new KafkaTopologyGraph(nodes.Values.ToList(), edges);
    }

    // "*" (tüm gruplar) yetkisi her kullanıcıda bulunan, bilgi taşımayan bir kayıt (Aiven tüm
    // kullanıcılara verir) — grafikte tek bir "*" düğümüne bağlanan kalabalık kenarlar üretirdi.
    private static bool IsWildcardLiteral(KafkaAclEntry entry) =>
        entry.ResourceName == "*" && !string.Equals(entry.PatternType, "Prefixed", StringComparison.Ordinal);
}
