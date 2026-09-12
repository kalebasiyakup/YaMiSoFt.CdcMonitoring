using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Domain.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CdcMonitoring.Infrastructure.Email;

public class MailKitEmailNotifier(
    ISystemSettingsRepository settingsRepository,
    ISmtpPasswordProtector smtpPasswordProtector,
    ILogger<MailKitEmailNotifier> logger) : IEmailNotifier
{
    public Task SendAlertAsync(AlertSeverity severity, string message, CancellationToken ct = default)
    {
        var subject = severity switch
        {
            AlertSeverity.Critical => "[KRİTİK] CDC Monitoring Alarmı",
            AlertSeverity.Warning => "[UYARI] CDC Monitoring Alarmı",
            _ => "[BİLGİ] CDC Monitoring Bildirimi"
        };

        return SendAsync(subject, message, ct);
    }

    public Task SendReportAsync(string subject, string body, CancellationToken ct = default) =>
        SendAsync(subject, body, ct);

    private async Task SendAsync(string subject, string body, CancellationToken ct)
    {
        var settings = await settingsRepository.GetAsync(ct);
        var recipients = settings.RecipientsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (!settings.EmailEnabled || recipients.Length == 0 || string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            logger.LogWarning("E-posta gönderimi atlandı (SMTP yapılandırılmamış veya devre dışı): {Subject}", subject);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromDisplayName, settings.FromAddress));
        foreach (var recipient in recipients)
            message.To.Add(MailboxAddress.Parse(recipient));

        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort,
            settings.SmtpUseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto, ct);

        if (!string.IsNullOrWhiteSpace(settings.SmtpUsername) && !string.IsNullOrEmpty(settings.EncryptedSmtpPassword))
        {
            var plaintextPassword = smtpPasswordProtector.Unprotect(settings.EncryptedSmtpPassword);
            await client.AuthenticateAsync(settings.SmtpUsername, plaintextPassword, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation("E-posta gönderildi: {Subject} ({RecipientCount} alıcı)", subject, recipients.Length);
    }
}
