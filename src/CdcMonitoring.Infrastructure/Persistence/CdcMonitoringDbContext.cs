using CdcMonitoring.Domain.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CdcMonitoring.Infrastructure.Persistence;

public class CdcMonitoringDbContext(DbContextOptions<CdcMonitoringDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<PgConnection> PgConnections => Set<PgConnection>();
    public DbSet<ConnectionHealthCheck> ConnectionHealthChecks => Set<ConnectionHealthCheck>();
    public DbSet<CdcRelationship> CdcRelationships => Set<CdcRelationship>();
    public DbSet<CdcRelationshipHealth> CdcRelationshipHealthEntries => Set<CdcRelationshipHealth>();
    public DbSet<AlertEvent> AlertEvents => Set<AlertEvent>();
    public DbSet<ReconciliationResult> ReconciliationResults => Set<ReconciliationResult>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();
    public DbSet<JobSchedule> JobSchedules => Set<JobSchedule>();

    public DbSet<SchemaScan> SchemaScans => Set<SchemaScan>();
    public DbSet<DbTable> DbTables => Set<DbTable>();
    public DbSet<DbColumn> DbColumns => Set<DbColumn>();
    public DbSet<DbIndex> DbIndexes => Set<DbIndex>();
    public DbSet<DbConstraint> DbConstraints => Set<DbConstraint>();
    public DbSet<SchemaChangeEvent> SchemaChangeEvents => Set<SchemaChangeEvent>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CdcMonitoringDbContext).Assembly);
    }
}
