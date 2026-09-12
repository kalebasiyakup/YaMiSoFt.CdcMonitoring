using System.Diagnostics;
using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Common;
using CdcMonitoring.Domain.Entities;
using Npgsql;

namespace CdcMonitoring.Infrastructure.Postgres;

public class NpgsqlConnectivityChecker : IPostgresConnectivityChecker
{
    // FR-14: bu sınıfın izlenen PostgreSQL örneklerine karşı ürettiği tek sorgu budur;
    // salt-okuma garantisi CdcMonitoring.UnitTests/Postgres altındaki guard test ile doğrulanır.
    internal const string HealthCheckQuery = "SELECT version();";

    public async Task<ConnectivityCheckResult> CheckAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = connection.Host,
            Port = connection.Port,
            Database = connection.DatabaseName,
            Username = connection.Username,
            Password = plaintextPassword,
            SslMode = PgSslModeMapper.ToNpgsql(connection.SslMode),
            TrustServerCertificate = connection.TrustServerCertificate,
            // Npgsql'in kendi bağlantı/komut zaman aşımı burada sabitlenmez: gerçek üst sınır
            // çağıranın (ConnectionHealthCheckService) SystemSettings.HealthCheckTimeoutSeconds'tan
            // türettiği ve conn.OpenAsync/ExecuteScalarAsync'e geçirdiği CancellationToken'dır.
            // Sabit düşük bir değer burada kalsaydı, admin'in ayarladığı daha uzun bir zaman
            // aşımı sessizce görmezden gelinirdi.
            Timeout = 0,
            CommandTimeout = 0
        };

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var conn = new NpgsqlConnection(builder.ConnectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new NpgsqlCommand(HealthCheckQuery, conn);
            var version = (string?)await cmd.ExecuteScalarAsync(ct);

            stopwatch.Stop();
            return new ConnectivityCheckResult(true, stopwatch.Elapsed.TotalMilliseconds, version, null);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new ConnectivityCheckResult(false, null, null, ex.Message);
        }
    }
}
