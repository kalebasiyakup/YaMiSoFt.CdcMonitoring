using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public required string EntityType { get; set; }
    public Guid EntityId { get; set; }
    public AuditAction Action { get; set; }
    public required string ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}
