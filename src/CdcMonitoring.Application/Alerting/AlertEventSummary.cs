using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.Alerting;

public record AlertEventSummary(
    Guid Id,
    AlertType Type,
    AlertSeverity Severity,
    string? ConnectionName,
    string? RelationshipLabel,
    DateTimeOffset TriggeredAt,
    DateTimeOffset? NotifiedAt,
    DateTimeOffset? ResolvedAt,
    string Message);
