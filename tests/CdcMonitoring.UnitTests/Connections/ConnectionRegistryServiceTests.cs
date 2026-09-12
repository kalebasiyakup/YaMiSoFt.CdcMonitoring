using CdcMonitoring.Application.Common;
using CdcMonitoring.Application.Connections;
using CdcMonitoring.Domain.Enums;
using CdcMonitoring.UnitTests.TestDoubles;
using Xunit;

namespace CdcMonitoring.UnitTests.Connections;

public class ConnectionRegistryServiceTests
{
    private static (ConnectionRegistryService Service, InMemoryPgConnectionRepository Connections, InMemoryAuditLogRepository AuditLog) CreateService(
        Func<CdcMonitoring.Domain.Entities.PgConnection, ConnectivityCheckResult>? connectivityResultFactory = null)
    {
        var connections = new InMemoryPgConnectionRepository();
        var auditLog = new InMemoryAuditLogRepository();

        var service = new ConnectionRegistryService(
            connections,
            auditLog,
            new FakePasswordProtector(),
            new FakeConnectivityChecker(connectivityResultFactory ?? (_ => new ConnectivityCheckResult(true, 1, "PostgreSQL 16", null))),
            new FixedCurrentUserAccessor("yakup.kalebasi"),
            new FixedClock(DateTimeOffset.Parse("2026-09-12T10:00:00Z")));

        return (service, connections, auditLog);
    }

    private static CreateConnectionRequest ValidRequest(string name = "orders-db") => new(
        name, "db1.internal", 5432, "orders", "monitor_ro", "s3cr3t!", PgSslMode.Prefer, false, "Prod-DC1", "Sipariş veritabanı");

    [Fact]
    public async Task CreateAsync_persists_connection_and_writes_audit_log_without_password()
    {
        var (service, _, auditLog) = CreateService();

        var summary = await service.CreateAsync(ValidRequest());

        Assert.NotEqual(Guid.Empty, summary.Id);
        Assert.Equal("orders-db", summary.Name);

        var auditEntry = Assert.Single(auditLog.Entries);
        Assert.DoesNotContain("s3cr3t!", auditEntry.NewValue);
        Assert.DoesNotContain("enc:", auditEntry.NewValue);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_host_port_database()
    {
        var (service, _, _) = CreateService();
        await service.CreateAsync(ValidRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(ValidRequest("orders-db-2")));
    }

    [Fact]
    public async Task UpdateAsync_without_new_password_keeps_existing_encrypted_password()
    {
        var (service, connections, _) = CreateService();
        var created = await service.CreateAsync(ValidRequest());

        var stored = await connections.GetByIdAsync(created.Id);
        var encryptedBefore = stored!.EncryptedPassword;

        await service.UpdateAsync(new UpdateConnectionRequest(
            created.Id, "orders-db-renamed", "db1.internal", 5432, "orders", "monitor_ro",
            NewPlaintextPassword: null, PgSslMode.Prefer, TrustServerCertificate: false, "Prod-DC1", "Güncellendi", IsActive: true));

        var afterUpdate = await connections.GetByIdAsync(created.Id);
        Assert.Equal("orders-db-renamed", afterUpdate!.Name);
        Assert.Equal(encryptedBefore, afterUpdate.EncryptedPassword);
    }

    [Fact]
    public async Task TestConnectionAsync_returns_checker_result_when_password_provided()
    {
        var (service, _, _) = CreateService(
            connectivityResultFactory: c => new ConnectivityCheckResult(true, 12, "PostgreSQL 16.1", null));

        var result = await service.TestConnectionAsync(new TestConnectionRequest(
            "db1.internal", 5432, "orders", "monitor_ro", "s3cr3t!", PgSslMode.Require, TrustServerCertificate: true, ExistingConnectionId: null));

        Assert.True(result.IsUp);
        Assert.Equal("PostgreSQL 16.1", result.PostgresVersion);
    }

    [Fact]
    public async Task TestConnectionAsync_surfaces_failure_from_checker()
    {
        var (service, _, _) = CreateService(
            connectivityResultFactory: _ => new ConnectivityCheckResult(false, null, null, "connection refused"));

        var result = await service.TestConnectionAsync(new TestConnectionRequest(
            "db1.internal", 5432, "orders", "monitor_ro", "wrong", PgSslMode.Prefer, TrustServerCertificate: false, ExistingConnectionId: null));

        Assert.False(result.IsUp);
        Assert.Equal("connection refused", result.ErrorMessage);
    }

    [Fact]
    public async Task TestConnectionAsync_with_blank_password_reuses_existing_connection_password()
    {
        var (service, connections, _) = CreateService();
        var created = await service.CreateAsync(ValidRequest());

        string? passwordSeenByChecker = null;
        var probingService = new ConnectionRegistryService(
            connections,
            new InMemoryAuditLogRepository(),
            new FakePasswordProtector(),
            new CapturingConnectivityChecker((_, password) => passwordSeenByChecker = password),
            new FixedCurrentUserAccessor("yakup.kalebasi"),
            new FixedClock(DateTimeOffset.Parse("2026-09-12T10:00:00Z")));

        await probingService.TestConnectionAsync(new TestConnectionRequest(
            "db1.internal", 5432, "orders", "monitor_ro", PlaintextPassword: null, PgSslMode.Prefer, TrustServerCertificate: false, ExistingConnectionId: created.Id));

        Assert.Equal("s3cr3t!", passwordSeenByChecker);
    }

    [Fact]
    public async Task TestConnectionAsync_throws_when_no_password_and_no_existing_connection()
    {
        var (service, _, _) = CreateService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.TestConnectionAsync(new TestConnectionRequest(
            "db1.internal", 5432, "orders", "monitor_ro", PlaintextPassword: null, PgSslMode.Prefer, TrustServerCertificate: false, ExistingConnectionId: null)));
    }

    [Fact]
    public async Task DeleteAsync_removes_connection_and_writes_audit_log()
    {
        var (service, connections, auditLog) = CreateService();
        var created = await service.CreateAsync(ValidRequest());

        await service.DeleteAsync(created.Id);

        Assert.Null(await connections.GetByIdAsync(created.Id));
        Assert.Contains(auditLog.Entries, e => e.Action == CdcMonitoring.Domain.Enums.AuditAction.Deleted);
    }
}
