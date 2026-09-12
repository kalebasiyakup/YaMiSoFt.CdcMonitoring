using System.Collections.Concurrent;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class RecordingMetricsRecorder : IMetricsRecorder
{
    public ConcurrentBag<(string ConnectionName, string EnvironmentTag, bool IsUp, double? LatencyMs)> HealthRecords { get; } = [];
    public ConcurrentBag<(string JobName, double DurationSeconds)> ScanCycleRecords { get; } = [];
    public ConcurrentBag<(string Source, string Target, string Slot, bool SlotActive, long? LagBytes, SubscriptionState State)> CdcHealthRecords { get; } = [];

    public void RecordConnectionHealth(string connectionName, string environmentTag, bool isUp, double? latencyMs) =>
        HealthRecords.Add((connectionName, environmentTag, isUp, latencyMs));

    public void RecordScanCycleDuration(string jobName, double durationSeconds) =>
        ScanCycleRecords.Add((jobName, durationSeconds));

    public void RecordCdcRelationshipHealth(string sourceConnectionName, string targetConnectionName, string slotName, bool slotActive, long? lagBytes, SubscriptionState subscriptionState) =>
        CdcHealthRecords.Add((sourceConnectionName, targetConnectionName, slotName, slotActive, lagBytes, subscriptionState));
}
