namespace CdcMonitoring.Application.Connections;

public record CreateConnectionRequest(
    string Name,
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string PlaintextPassword,
    string EnvironmentTag,
    string? Description);

public record UpdateConnectionRequest(
    Guid Id,
    string Name,
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string? NewPlaintextPassword,
    string EnvironmentTag,
    string? Description,
    bool IsActive);

public record ConnectionSummary(
    Guid Id,
    string Name,
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string EnvironmentTag,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);
