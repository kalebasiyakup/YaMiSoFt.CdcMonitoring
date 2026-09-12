namespace CdcMonitoring.Domain.Entities;

public class ReconciliationResult
{
    public Guid Id { get; set; }
    public Guid RelationshipId { get; set; }
    public CdcRelationship? Relationship { get; set; }
    public DateTimeOffset RunAt { get; set; }
    public long? SourceRowCount { get; set; }
    public long? TargetRowCount { get; set; }
    public string? SourceChecksum { get; set; }
    public string? TargetChecksum { get; set; }
    public bool IsMatch { get; set; }
    public string? Details { get; set; }
}
