using CdcMonitoring.Application.Abstractions;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class MutableClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}
