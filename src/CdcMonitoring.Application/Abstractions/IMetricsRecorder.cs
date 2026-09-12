using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.Abstractions;

public interface IMetricsRecorder
{
    void RecordConnectionHealth(string connectionName, string environmentTag, bool isUp, double? latencyMs);
    void RecordScanCycleDuration(string jobName, double durationSeconds);
    void RecordCdcRelationshipHealth(string sourceConnectionName, string targetConnectionName, string slotName, bool slotActive, long? lagBytes, SubscriptionState subscriptionState);
}
