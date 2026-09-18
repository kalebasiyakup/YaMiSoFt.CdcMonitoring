using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.KafkaAudit;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class FakeKafkaAdminGateway : IKafkaAdminGateway
{
    public List<KafkaTopicSummary> Topics { get; } = [];

    public KafkaAclQueryOutcome AclOutcome { get; set; } = new(true, [], null);

    public KafkaConsumerGroupQueryOutcome GroupOutcome { get; set; } = new(true, [], null);

    public Task<IReadOnlyList<KafkaTopicSummary>> GetTopicsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<KafkaTopicSummary>>(Topics);

    public Task<KafkaAclQueryOutcome> GetAclEntriesAsync(CancellationToken ct = default) =>
        Task.FromResult(AclOutcome);

    public Task<KafkaConsumerGroupQueryOutcome> GetActiveConsumerGroupSubscriptionsAsync(CancellationToken ct = default) =>
        Task.FromResult(GroupOutcome);
}
