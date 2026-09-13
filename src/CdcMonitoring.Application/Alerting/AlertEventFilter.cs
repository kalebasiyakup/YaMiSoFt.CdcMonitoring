using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.Alerting;

// OnlyActive: null = tümü, true = yalnızca aktif (ResolvedAt == null), false = yalnızca çözülmüş.
public record AlertEventFilter(AlertType? Type, AlertSeverity? Severity, bool? OnlyActive);
