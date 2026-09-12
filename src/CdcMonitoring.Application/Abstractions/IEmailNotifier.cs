using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.Abstractions;

public interface IEmailNotifier
{
    Task SendAlertAsync(AlertSeverity severity, string message, CancellationToken ct = default);
    Task SendReportAsync(string subject, string body, CancellationToken ct = default);
}
