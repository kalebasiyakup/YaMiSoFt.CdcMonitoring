using CdcMonitoring.Domain.Enums;

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
    public PgSslMode SslMode { get; set; } = PgSslMode.Prefer;
    // Yalnızca SslMode=Require ile anlamlıdır: Npgsql, Require altında sunucu sertifikasını
    // doğrulamaz; bu bayrak kendi-imzalı sertifikalı iç ağ PostgreSQL örnekleri için TrustServerCertificate'i
    // açar (VerifyCA/VerifyFull zaten her koşulda doğrulama yapar, bu bayraktan etkilenmez).
    public bool TrustServerCertificate { get; set; }
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
