using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.KafkaAudit;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace CdcMonitoring.Infrastructure.Kafka;

/// <summary>
/// Confluent.Kafka AdminClient üzerinden Strimzi Kafka cluster'ını salt-okuma sorgular.
/// Hiçbir metot topic/ACL/consumer group oluşturmaz, silmez veya değiştirmez.
/// </summary>
public class ConfluentKafkaAdminGateway(KafkaConnectionOptions options, ILogger<ConfluentKafkaAdminGateway> logger) : IKafkaAdminGateway
{
    private TimeSpan Timeout => TimeSpan.FromSeconds(options.RequestTimeoutSeconds);

    public Task<IReadOnlyList<KafkaTopicSummary>> GetTopicsAsync(CancellationToken ct = default)
    {
        return Task.Run<IReadOnlyList<KafkaTopicSummary>>(() =>
        {
            using var admin = KafkaAdminClientFactory.Create(options);
            var metadata = admin.GetMetadata(Timeout);

            return metadata.Topics
                .Where(t => t.Error.Code == ErrorCode.NoError)
                .Select(t => new KafkaTopicSummary(
                    t.Topic,
                    t.Partitions.Count,
                    t.Partitions.Count == 0 ? 0 : t.Partitions[0].Replicas.Length))
                .ToList();
        }, ct);
    }

    public async Task<KafkaAclQueryOutcome> GetAclEntriesAsync(CancellationToken ct = default)
    {
        try
        {
            using var admin = KafkaAdminClientFactory.Create(options);

            // Any/Any/Any: cluster'daki TÜM kaynak tipleri (Topic/Group/Cluster/TransactionalId)
            // için TÜM ACL kayıtlarını getir — Allow/Deny ve işlem (operation) filtrelemesi
            // Application katmanında (KafkaTopicAccessReportService) yapılır.
            var filter = new AclBindingFilter
            {
                PatternFilter = new ResourcePatternFilter
                {
                    Type = ResourceType.Any,
                    ResourcePatternType = ResourcePatternType.Any
                },
                EntryFilter = new AccessControlEntryFilter
                {
                    Operation = AclOperation.Any,
                    PermissionType = AclPermissionType.Any
                }
            };

            var result = await admin.DescribeAclsAsync(filter, new DescribeAclsOptions { RequestTimeout = Timeout });

            var entries = result.AclBindings
                .Select(b => new KafkaAclEntry(
                    b.Entry.Principal,
                    b.Entry.PermissionType.ToString(),
                    ResourceTypeName(b.Pattern.Type),
                    b.Pattern.Name,
                    b.Entry.Operation.ToString(),
                    b.Pattern.ResourcePatternType.ToString()))
                .ToList();

            return new KafkaAclQueryOutcome(true, entries, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kafka ACL bilgisi alınamadı (authorizer devre dışı veya yetki yetersiz olabilir).");
            return new KafkaAclQueryOutcome(false, [], ex.Message);
        }
    }

    // Confluent.Kafka 2.6.1'in ResourceType enum'ı yalnızca Unknown/Any/Topic/Group/Broker(=4)
    // isimlerini tanımlar; gerçek Kafka protokolündeki TransactionalId(=5)/DelegationToken(=6)/
    // User(=7) için isim yok — tanımsız değerlerde .ToString() ham sayıyı döner (ör. "5").
    // Ayrıca "Broker" ismi Kafka'nın kendi terminolojisiyle uyuşmuyor (gerçek adı "Cluster").
    private static string ResourceTypeName(ResourceType type) => (int)type switch
    {
        0 => "Unknown",
        1 => "Any",
        2 => "Topic",
        3 => "Group",
        4 => "Cluster",
        5 => "TransactionalId",
        6 => "DelegationToken",
        7 => "User",
        _ => type.ToString()
    };

    public async Task<KafkaConsumerGroupQueryOutcome> GetActiveConsumerGroupSubscriptionsAsync(CancellationToken ct = default)
    {
        try
        {
            using var admin = KafkaAdminClientFactory.Create(options);

            var groupIds = await Task.Run(() => admin.ListGroups(Timeout).Select(g => g.Group).ToList(), ct);
            if (groupIds.Count == 0)
                return new KafkaConsumerGroupQueryOutcome(true, [], null);

            var described = await admin.DescribeConsumerGroupsAsync(groupIds, new DescribeConsumerGroupsOptions { RequestTimeout = Timeout });

            // Yalnızca State=Stable VE en az bir topic'e fiilen atanmış partition'ı olan gruplar
            // "şu an gerçekten abone" kabul edilir (rebalance ortasındaki boş atamalar hariç).
            var subscriptions = described.ConsumerGroupDescriptions
                .Where(g => !g.Error.IsError && g.State == ConsumerGroupState.Stable)
                .Select(g => new KafkaConsumerGroupSubscription(
                    g.GroupId,
                    g.State.ToString(),
                    g.Members
                        .SelectMany(m => m.Assignment?.TopicPartitions ?? [])
                        .Select(tp => tp.Topic)
                        .Distinct(StringComparer.Ordinal)
                        .ToList()))
                .Where(s => s.SubscribedTopics.Count > 0)
                .ToList();

            return new KafkaConsumerGroupQueryOutcome(true, subscriptions, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Kafka consumer group bilgisi alınamadı.");
            return new KafkaConsumerGroupQueryOutcome(false, [], ex.Message);
        }
    }
}
