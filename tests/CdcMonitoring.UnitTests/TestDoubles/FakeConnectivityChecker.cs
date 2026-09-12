using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Common;
using CdcMonitoring.Domain.Entities;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class FakeConnectivityChecker(Func<PgConnection, ConnectivityCheckResult> resultFactory) : IPostgresConnectivityChecker
{
    public Task<ConnectivityCheckResult> CheckAsync(PgConnection connection, string plaintextPassword, CancellationToken ct = default) =>
        Task.FromResult(resultFactory(connection));
}
