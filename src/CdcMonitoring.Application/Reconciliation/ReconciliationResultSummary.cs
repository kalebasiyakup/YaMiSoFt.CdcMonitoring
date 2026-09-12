namespace CdcMonitoring.Application.Reconciliation;

public record ReconciliationResultSummary(
    Guid Id,
    string SourceConnectionName,
    string TargetConnectionName,
    string SlotName,
    DateTimeOffset RunAt,
    long? SourceRowCount,
    long? TargetRowCount,
    bool IsMatch,
    string? Details);
