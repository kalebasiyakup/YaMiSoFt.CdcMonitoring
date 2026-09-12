using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Domain.Entities;

public class AlertEvent
{
    public Guid Id { get; set; }
    public AlertType Type { get; set; }
    public AlertSeverity Severity { get; set; }
    public Guid? ConnectionId { get; set; }
    public Guid? RelationshipId { get; set; }
    public DateTimeOffset TriggeredAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public DateTimeOffset? NotifiedAt { get; set; }
    public required string Message { get; set; }

    public bool IsActive => ResolvedAt is null;
}
