using CdcMonitoring.Application.Connections;
using CdcMonitoring.UnitTests.TestDoubles;
using Xunit;

namespace CdcMonitoring.UnitTests.Connections;

public class ConnectionRegistryServiceTests
{
    private static (ConnectionRegistryService Service, InMemoryPgConnectionRepository Connections, InMemoryAuditLogRepository AuditLog) CreateService()
    {
        var connections = new InMemoryPgConnectionRepository();
        var auditLog = new InMemoryAuditLogRepository();

        var service = new ConnectionRegistryService(
            connections,
            auditLog,
            new FakePasswordProtector(),
            new FixedCurrentUserAccessor("yakup.kalebasi"),
            new FixedClock(DateTimeOffset.Parse("2026-09-12T10:00:00Z")));

        return (service, connections, auditLog);
    }

    private static CreateConnectionRequest ValidRequest(string name = "orders-db") => new(
        name, "db1.internal", 5432, "orders", "monitor_ro", "s3cr3t!", "Prod-DC1", "Sipariş veritabanı");

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
            NewPlaintextPassword: null, "Prod-DC1", "Güncellendi", IsActive: true));

        var afterUpdate = await connections.GetByIdAsync(created.Id);
        Assert.Equal("orders-db-renamed", afterUpdate!.Name);
        Assert.Equal(encryptedBefore, afterUpdate.EncryptedPassword);
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
