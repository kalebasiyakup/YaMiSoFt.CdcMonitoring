using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.UnitTests.TestDoubles;

public class FakeEmailNotifier : IEmailNotifier
{
    public List<(AlertSeverity Severity, string Message)> Alerts { get; } = [];
    public List<(string Subject, string Body)> Reports { get; } = [];

    public Task SendAlertAsync(AlertSeverity severity, string message, CancellationToken ct = default)
    {
        Alerts.Add((severity, message));
        return Task.CompletedTask;
    }

    public Task SendReportAsync(string subject, string body, CancellationToken ct = default)
    {
        Reports.Add((subject, body));
        return Task.CompletedTask;
    }
}
