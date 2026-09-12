using CdcMonitoring.Application.Common;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.Connections;

public record CreateConnectionRequest(
    string Name,
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string PlaintextPassword,
    PgSslMode SslMode,
    bool TrustServerCertificate,
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
    PgSslMode SslMode,
    bool TrustServerCertificate,
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
    PgSslMode SslMode,
    bool TrustServerCertificate,
    string EnvironmentTag,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    DateTimeOffset? UpdatedAt,
    string? UpdatedBy);

// Kaydetmeden önce bağlantıyı test etmek için (FR-03): kayıtlı bir PgConnection gerektirmez.
// Parola boşsa ve ExistingConnectionId verilmişse, o bağlantının kayıtlı şifreli parolası kullanılır
// (Edit ekranında parola alanı boş bırakılıp yalnızca SSL/host gibi diğer alanlar değiştiğinde).
public record TestConnectionRequest(
    string Host,
    int Port,
    string DatabaseName,
    string Username,
    string? PlaintextPassword,
    PgSslMode SslMode,
    bool TrustServerCertificate,
    Guid? ExistingConnectionId);
