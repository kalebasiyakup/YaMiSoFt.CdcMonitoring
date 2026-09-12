namespace CdcMonitoring.Application.Common;

public record ConnectivityCheckResult(bool IsUp, double? LatencyMs, string? PostgresVersion, string? ErrorMessage);
