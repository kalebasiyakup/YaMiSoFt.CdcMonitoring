using CdcMonitoring.Application.Abstractions;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class FixedCurrentUserAccessor(string userName) : ICurrentUserAccessor
{
    public string GetCurrentUserNameOrDefault(string fallback = "system") => userName;
}
