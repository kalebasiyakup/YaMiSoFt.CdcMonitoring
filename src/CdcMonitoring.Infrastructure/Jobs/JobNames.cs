namespace CdcMonitoring.Infrastructure.Jobs;

public static class JobNames
{
    public const string ConnectionHealthCheck = "connection-health-check";
    public const string CdcDiscovery = "cdc-discovery";
    public const string AlertEvaluation = "alert-evaluation";
    public const string WeeklyReconciliation = "weekly-reconciliation";
    public const string SchemaCatalogScan = "schema-catalog-scan";
    public const string RetentionCleanup = "retention-cleanup";
}
