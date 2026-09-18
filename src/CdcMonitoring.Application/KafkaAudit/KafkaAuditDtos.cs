namespace CdcMonitoring.Application.KafkaAudit;

public record KafkaTopicSummary(string Name, int PartitionCount, int ReplicationFactor);

/// <summary>
/// Ham bir ACL girdisi — yalnızca Topic değil, cluster'daki TÜM kaynak tipleri (Topic/Group/
/// Cluster/TransactionalId) için. Operation değerleri Confluent.Kafka'nın AclOperation enum
/// isimleriyle birebir aynıdır (Write/Read/All/Describe/Create/Delete/Alter/ClusterAction/
/// DescribeConfigs/AlterConfigs/IdempotentWrite).
/// </summary>
public record KafkaAclEntry(
    string Principal,
    string PermissionType, // "Allow" | "Deny"
    string ResourceType,   // "Topic" | "Group" | "Cluster" | "TransactionalId" | ...
    string ResourceName,
    string Operation,
    string PatternType);   // "Literal" | "Prefixed" | ...

/// <summary>
/// ACL sorgusunun sonucu. Authorizer kapalıysa (veya erişim reddedilirse) Succeeded=false
/// döner ve Entries boş kalır — çağıran bunu hata değil, uyarı olarak ele almalıdır.
/// </summary>
public record KafkaAclQueryOutcome(bool Succeeded, IReadOnlyList<KafkaAclEntry> Entries, string? Error);

public record KafkaConsumerGroupSubscription(string GroupId, string State, IReadOnlyList<string> SubscribedTopics);

/// <summary>Bkz. <see cref="KafkaAclQueryOutcome"/> — aynı "başarısızsa uyar, devam et" sözleşmesi.</summary>
public record KafkaConsumerGroupQueryOutcome(bool Succeeded, IReadOnlyList<KafkaConsumerGroupSubscription> Groups, string? Error);

public record KafkaTopicAccessReportRow(
    string TopicName,
    int PartitionCount,
    int ReplicationFactor,
    IReadOnlyList<string> WritePrincipals,
    IReadOnlyList<string> ReadPrincipals,
    IReadOnlyList<string> ActiveConsumerGroups);

/// <param name="AclEntries">Tüm kaynak tiplerini kapsayan, izin verilmiş (Allow) ham ACL girdileri — detaylı denetim tablosu için.</param>
public record KafkaTopicAccessReport(
    IReadOnlyList<KafkaTopicAccessReportRow> Topics,
    IReadOnlyList<KafkaAclEntry> AclEntries,
    IReadOnlyList<string> Warnings,
    DateTimeOffset GeneratedAtUtc);
