using CdcMonitoring.Domain.Enums;
using Npgsql;

namespace CdcMonitoring.Infrastructure.Postgres;

internal static class PgSslModeMapper
{
    public static SslMode ToNpgsql(PgSslMode mode) => mode switch
    {
        PgSslMode.Disable => SslMode.Disable,
        PgSslMode.Allow => SslMode.Allow,
        PgSslMode.Prefer => SslMode.Prefer,
        PgSslMode.Require => SslMode.Require,
        PgSslMode.VerifyCA => SslMode.VerifyCA,
        PgSslMode.VerifyFull => SslMode.VerifyFull,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
    };
}
