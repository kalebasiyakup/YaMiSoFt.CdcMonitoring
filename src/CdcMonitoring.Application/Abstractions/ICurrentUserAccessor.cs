namespace CdcMonitoring.Application.Abstractions;

public interface ICurrentUserAccessor
{
    string GetCurrentUserNameOrDefault(string fallback = "system");
}
