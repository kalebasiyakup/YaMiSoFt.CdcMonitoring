using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Domain.Entities;

public class CdcRelationship
{
    public Guid Id { get; set; }
    public Guid SourceConnectionId { get; set; }
    public PgConnection? SourceConnection { get; set; }
    public Guid TargetConnectionId { get; set; }
    public PgConnection? TargetConnection { get; set; }
    public required string PublicationName { get; set; }
    public required string SubscriptionName { get; set; }
    public required string SlotName { get; set; }
    public CdcRelationshipStatus Status { get; set; } = CdcRelationshipStatus.Inferred;
    public string? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public List<CdcRelationshipHealth> HealthHistory { get; set; } = [];
    public List<ReconciliationResult> ReconciliationResults { get; set; } = [];
}
