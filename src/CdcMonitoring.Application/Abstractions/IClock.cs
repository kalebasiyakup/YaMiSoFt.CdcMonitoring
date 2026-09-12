namespace CdcMonitoring.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
