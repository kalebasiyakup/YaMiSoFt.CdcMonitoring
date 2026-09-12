using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Enums;
using Prometheus;

namespace CdcMonitoring.Infrastructure.Metrics;

public class PrometheusMetricsRecorder : IMetricsRecorder
{
    private static readonly Gauge ConnectionUp = Prometheus.Metrics.CreateGauge(
        "pgconn_up", "Bağlantının en son health check'te erişilebilir olup olmadığı (1/0).",
        new GaugeConfiguration { LabelNames = ["connection", "environment"] });

    private static readonly Gauge ConnectionLatencyMs = Prometheus.Metrics.CreateGauge(
        "pgconn_latency_ms", "Bağlantının en son health check gecikmesi (ms).",
        new GaugeConfiguration { LabelNames = ["connection", "environment"] });

    private static readonly Gauge ScanCycleDurationSeconds = Prometheus.Metrics.CreateGauge(
        "scan_cycle_duration_seconds", "Tarama job'unun son tur süresi (sn).",
        new GaugeConfiguration { LabelNames = ["job"] });

    private static readonly Gauge CdcSlotLagBytes = Prometheus.Metrics.CreateGauge(
        "cdc_slot_lag_bytes", "CDC replication slot'unun geriden gelme miktarı (byte).",
        new GaugeConfiguration { LabelNames = ["source", "target", "slot"] });

    private static readonly Gauge CdcSlotActive = Prometheus.Metrics.CreateGauge(
        "cdc_slot_active", "CDC replication slot'unun aktif olup olmadığı (1/0).",
        new GaugeConfiguration { LabelNames = ["source", "target", "slot"] });

    private static readonly Gauge CdcSubscriptionState = Prometheus.Metrics.CreateGauge(
        "cdc_subscription_state", "CDC subscription durumu (1=enabled, 0.5=error, 0=disabled).",
        new GaugeConfiguration { LabelNames = ["source", "target", "slot"] });

    public void RecordConnectionHealth(string connectionName, string environmentTag, bool isUp, double? latencyMs)
    {
        ConnectionUp.WithLabels(connectionName, environmentTag).Set(isUp ? 1 : 0);
        if (latencyMs.HasValue)
            ConnectionLatencyMs.WithLabels(connectionName, environmentTag).Set(latencyMs.Value);
    }

    public void RecordScanCycleDuration(string jobName, double durationSeconds) =>
        ScanCycleDurationSeconds.WithLabels(jobName).Set(durationSeconds);

    public void RecordCdcRelationshipHealth(string sourceConnectionName, string targetConnectionName, string slotName, bool slotActive, long? lagBytes, SubscriptionState subscriptionState)
    {
        CdcSlotActive.WithLabels(sourceConnectionName, targetConnectionName, slotName).Set(slotActive ? 1 : 0);
        if (lagBytes.HasValue)
            CdcSlotLagBytes.WithLabels(sourceConnectionName, targetConnectionName, slotName).Set(lagBytes.Value);

        var stateValue = subscriptionState switch
        {
            SubscriptionState.Enabled => 1,
            SubscriptionState.Error => 0.5,
            _ => 0
        };
        CdcSubscriptionState.WithLabels(sourceConnectionName, targetConnectionName, slotName).Set(stateValue);
    }
}
