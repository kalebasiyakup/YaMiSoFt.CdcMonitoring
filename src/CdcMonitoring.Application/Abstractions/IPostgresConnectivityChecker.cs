using CdcMonitoring.Application.Common;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.Application.Abstractions;

/// <summary>
/// Salt-okuma bağlanabilirlik testi (FR-03). Bu arayüzün hiçbir implementasyonu
/// izlenen PostgreSQL örneklerinde DDL/DML çalıştırmamalıdır (FR-14).
/// </summary>
public interface IPostgresConnectivityChecker
{
    Task<ConnectivityCheckResult> CheckAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default);
}
