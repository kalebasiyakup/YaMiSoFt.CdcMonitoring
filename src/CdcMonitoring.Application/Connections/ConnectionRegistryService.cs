using System.Text.Json;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Entities;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.Connections;

public class ConnectionRegistryService(
    IPgConnectionRepository connections,
    IAuditLogRepository auditLog,
    IConnectionPasswordProtector passwordProtector,
    ICurrentUserAccessor currentUser,
    IClock clock)
{
    public async Task<List<ConnectionSummary>> ListAsync(CancellationToken ct = default)
    {
        var all = await connections.GetAllAsync(ct);
        return all.Select(ToSummary).ToList();
    }

    public async Task<ConnectionSummary?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await connections.GetByIdAsync(id, ct);
        return connection is null ? null : ToSummary(connection);
    }

    public async Task<ConnectionSummary> CreateAsync(CreateConnectionRequest request, CancellationToken ct = default)
    {
        if (await connections.ExistsAsync(request.Host, request.Port, request.DatabaseName, ct: ct))
            throw new InvalidOperationException($"Bu host/port/veritabanı kombinasyonu ({request.Host}:{request.Port}/{request.DatabaseName}) zaten kayıtlı.");

        var actor = currentUser.GetCurrentUserNameOrDefault();
        var now = clock.UtcNow;

        var connection = new PgConnection
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Host = request.Host,
            Port = request.Port,
            DatabaseName = request.DatabaseName,
            Username = request.Username,
            EncryptedPassword = passwordProtector.Protect(request.PlaintextPassword),
            EnvironmentTag = request.EnvironmentTag,
            Description = request.Description,
            IsActive = true,
            CreatedAt = now,
            CreatedBy = actor
        };

        await connections.AddAsync(connection, ct);
        await connections.SaveChangesAsync(ct);

        await WriteAuditAsync(connection.Id, AuditAction.Created, actor, now, oldValue: null, newValue: ToAuditSnapshot(connection), ct);

        return ToSummary(connection);
    }

    public async Task<ConnectionSummary> UpdateAsync(UpdateConnectionRequest request, CancellationToken ct = default)
    {
        var connection = await connections.GetByIdAsync(request.Id, ct)
            ?? throw new KeyNotFoundException($"Bağlantı bulunamadı: {request.Id}");

        if (await connections.ExistsAsync(request.Host, request.Port, request.DatabaseName, request.Id, ct))
            throw new InvalidOperationException($"Bu host/port/veritabanı kombinasyonu ({request.Host}:{request.Port}/{request.DatabaseName}) başka bir kayıtla çakışıyor.");

        var actor = currentUser.GetCurrentUserNameOrDefault();
        var now = clock.UtcNow;
        var before = ToAuditSnapshot(connection);

        connection.Name = request.Name;
        connection.Host = request.Host;
        connection.Port = request.Port;
        connection.DatabaseName = request.DatabaseName;
        connection.Username = request.Username;
        connection.EnvironmentTag = request.EnvironmentTag;
        connection.Description = request.Description;
        connection.IsActive = request.IsActive;
        connection.UpdatedAt = now;
        connection.UpdatedBy = actor;

        if (!string.IsNullOrWhiteSpace(request.NewPlaintextPassword))
            connection.EncryptedPassword = passwordProtector.Protect(request.NewPlaintextPassword);

        await connections.SaveChangesAsync(ct);

        await WriteAuditAsync(connection.Id, AuditAction.Updated, actor, now, before, ToAuditSnapshot(connection), ct);

        return ToSummary(connection);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await connections.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Bağlantı bulunamadı: {id}");

        var actor = currentUser.GetCurrentUserNameOrDefault();
        var now = clock.UtcNow;
        var before = ToAuditSnapshot(connection);

        connections.Remove(connection);
        await connections.SaveChangesAsync(ct);

        await WriteAuditAsync(id, AuditAction.Deleted, actor, now, before, newValue: null, ct);
    }

    private async Task WriteAuditAsync(Guid entityId, AuditAction action, string actor, DateTimeOffset at, object? oldValue, object? newValue, CancellationToken ct)
    {
        await auditLog.AddAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = nameof(PgConnection),
            EntityId = entityId,
            Action = action,
            ChangedBy = actor,
            ChangedAt = at,
            OldValue = oldValue is null ? null : JsonSerializer.Serialize(oldValue),
            NewValue = newValue is null ? null : JsonSerializer.Serialize(newValue)
        }, ct);
    }

    // Parola bilinçli olarak dışarıda bırakılır: audit kaydı hiçbir zaman
    // düz metin veya şifreli parolayı içermemelidir (FR-02).
    private static object ToAuditSnapshot(PgConnection connection) => new
    {
        connection.Name,
        connection.Host,
        connection.Port,
        connection.DatabaseName,
        connection.Username,
        connection.EnvironmentTag,
        connection.Description,
        connection.IsActive
    };

    private static ConnectionSummary ToSummary(PgConnection connection) => new(
        connection.Id,
        connection.Name,
        connection.Host,
        connection.Port,
        connection.DatabaseName,
        connection.Username,
        connection.EnvironmentTag,
        connection.Description,
        connection.IsActive,
        connection.CreatedAt,
        connection.CreatedBy,
        connection.UpdatedAt,
        connection.UpdatedBy);
}
