namespace CdcMonitoring.Domain.Entities;

public class PgConnection
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Host { get; set; }
    public int Port { get; set; } = 5432;
    public required string DatabaseName { get; set; }
    public required string Username { get; set; }
    public required string EncryptedPassword { get; set; }
    public required string EnvironmentTag { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public required string CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public List<ConnectionHealthCheck> HealthChecks { get; set; } = [];
    public List<CdcRelationship> SourceRelationships { get; set; } = [];
    public List<CdcRelationship> TargetRelationships { get; set; } = [];
}
