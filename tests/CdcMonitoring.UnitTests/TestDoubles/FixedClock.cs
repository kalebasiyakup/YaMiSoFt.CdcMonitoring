using CdcMonitoring.Application.Abstractions;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; } = now;
}
