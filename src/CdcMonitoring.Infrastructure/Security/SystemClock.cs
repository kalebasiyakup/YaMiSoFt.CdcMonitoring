using CdcMonitoring.Application.Abstractions;

namespace CdcMonitoring.Infrastructure.Security;

public class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
