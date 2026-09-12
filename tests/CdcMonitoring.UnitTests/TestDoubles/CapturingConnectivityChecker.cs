using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Common;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class CapturingConnectivityChecker(Action<PgConnection, string> onCheck) : IPostgresConnectivityChecker
{
    public Task<ConnectivityCheckResult> CheckAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default)
    {
        onCheck(connection, plaintextPassword);
        return Task.FromResult(new ConnectivityCheckResult(true, 1, "PostgreSQL 16", null));
    }
}
