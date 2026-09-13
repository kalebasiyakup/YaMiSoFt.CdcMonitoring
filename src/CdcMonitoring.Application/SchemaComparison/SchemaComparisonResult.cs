namespace CdcMonitoring.Application.SchemaComparison;

public record ColumnTypeMismatch(string ColumnName, string SourceType, string TargetType);

public record TableSchemaDiff(
    string SchemaName,
    string TableName,
    List<string> MissingInTarget,
    List<string> ExtraInTarget,
    List<ColumnTypeMismatch> TypeMismatches)
{
    public bool HasDifferences => MissingInTarget.Count > 0 || ExtraInTarget.Count > 0 || TypeMismatches.Count > 0;
}

public record RelationshipSchemaComparison(
    Guid RelationshipId,
    string SourceConnectionName,
    string TargetConnectionName,
    List<TableSchemaDiff> Tables)
{
    public bool HasDifferences => Tables.Any(t => t.HasDifferences);
}
