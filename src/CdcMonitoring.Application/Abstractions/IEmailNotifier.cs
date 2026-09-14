using CdcMonitoring.Application.Common;
using CdcMonitoring.Domain.Enums;

namespace CdcMonitoring.Application.Abstractions;

public interface IEmailNotifier
{
    Task SendAlertAsync(AlertSeverity severity, string message, CancellationToken ct = default);

    // body, HTML olarak gönderilir (ör. ReconciliationService'in tutarlılık raporu şablonu).
    Task SendReportAsync(string subject, string htmlBody, CancellationToken ct = default);

    // FR-10 ekranından (Ayarlar > E-posta) kaydetmeden önce SMTP yapılandırmasını doğrulamak için:
    // verilen ayarlarla gerçek bir test e-postası gönderir, kalıcı hiçbir kayıt oluşturmaz.
    Task<EmailTestResult> SendTestEmailAsync(EmailTestRequest request, CancellationToken ct = default);
}

public record EmailTestRequest(
    string SmtpHost,
    int SmtpPort,
    bool SmtpUseStartTls,
    string? SmtpUsername,
    string PlaintextPassword,
    string FromAddress,
    string FromDisplayName,
    IReadOnlyList<string> Recipients);
