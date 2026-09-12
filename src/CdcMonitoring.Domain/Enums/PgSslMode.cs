namespace CdcMonitoring.Domain.Enums;

// Npgsql.SslMode ile birebir aynı adlandırma (Infrastructure katmanında doğrudan eşlenir).
public enum PgSslMode
{
    Disable,
    Allow,
    Prefer,
    Require,
    VerifyCA,
    VerifyFull
}
