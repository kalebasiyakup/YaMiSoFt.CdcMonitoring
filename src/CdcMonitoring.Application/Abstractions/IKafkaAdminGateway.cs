using CdcMonitoring.Application.KafkaAudit;

namespace CdcMonitoring.Application.Abstractions;

/// <summary>
/// Kafka cluster'ına AdminClient üzerinden salt-okuma erişim. Topic listesi zorunlu kabul
/// edilir (alınamazsa çağıran hata alır); ACL ve consumer group sorguları ise authorizer
/// kapalıyken veya yetki yetersizken başarısız olabileceğinden Outcome nesneleri üzerinden
/// "başarısız + neden" bilgisini döner, exception fırlatmaz.
/// </summary>
public interface IKafkaAdminGateway
{
    Task<IReadOnlyList<KafkaTopicSummary>> GetTopicsAsync(CancellationToken ct = default);

    /// <summary>Cluster'daki TÜM kaynak tipleri (Topic/Group/Cluster/TransactionalId) için ham ACL girdilerini döner.</summary>
    Task<KafkaAclQueryOutcome> GetAclEntriesAsync(CancellationToken ct = default);

    Task<KafkaConsumerGroupQueryOutcome> GetActiveConsumerGroupSubscriptionsAsync(CancellationToken ct = default);
}
