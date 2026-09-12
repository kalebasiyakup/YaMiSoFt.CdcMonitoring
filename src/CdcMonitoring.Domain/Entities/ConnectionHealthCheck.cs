namespace CdcMonitoring.Domain.Entities;

public class ConnectionHealthCheck
{
    public Guid Id { get; set; }
    public Guid ConnectionId { get; set; }
    public PgConnection? Connection { get; set; }
    public DateTimeOffset CheckedAt { get; set; }
    public bool IsUp { get; set; }
    public double? LatencyMs { get; set; }
    public string? PostgresVersion { get; set; }
    public string? ErrorMessage { get; set; }
}
