using CdcMonitoring.Application.Abstractions;
using CdcMonitoring.Application.Common;
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

        return SendAsync(subject, message, isHtml: false, ct);
    }

    public Task SendReportAsync(string subject, string htmlBody, CancellationToken ct = default) =>
        SendAsync(subject, htmlBody, isHtml: true, ct);

    public async Task<EmailTestResult> SendTestEmailAsync(EmailTestRequest request, CancellationToken ct = default)
    {
        if (request.Recipients.Count == 0)
            return new EmailTestResult(false, "En az bir alıcı girilmelidir.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(request.FromDisplayName, request.FromAddress));
        foreach (var recipient in request.Recipients)
        {
            try
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }
            catch (Exception ex)
            {
                return new EmailTestResult(false, $"Geçersiz alıcı adresi '{recipient}': {ex.Message}");
            }
        }

        message.Subject = "CDC Monitoring - Test E-postası";
        message.Body = new TextPart("plain")
        {
            Text = "Bu, CDC Monitoring Ayarlar ekranından gönderilen bir test e-postasıdır. " +
                   "Bu e-postayı alıyorsanız SMTP yapılandırmanız çalışıyor demektir."
        };

        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(request.SmtpHost, request.SmtpPort,
                ResolveSecureSocketOptions(request.SmtpUseStartTls, request.SmtpPort), ct);

            if (!string.IsNullOrWhiteSpace(request.SmtpUsername) && !string.IsNullOrEmpty(request.PlaintextPassword))
                await client.AuthenticateAsync(request.SmtpUsername, request.PlaintextPassword, ct);

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);

            logger.LogInformation("Test e-postası gönderildi ({RecipientCount} alıcı).", request.Recipients.Count);
            return new EmailTestResult(true, null);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Test e-postası gönderilemedi.");
            return new EmailTestResult(false, ex.Message);
        }
    }

    private async Task SendAsync(string subject, string body, bool isHtml, CancellationToken ct)
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
        {
            try
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }
            catch (Exception ex)
            {
                // Bir alıcının adresi hatalıysa tüm listeyi değil yalnızca o alıcıyı atla —
                // aksi halde tek bir yazım hatası kritik bir alarmın herkese ulaşmasını engeller.
                logger.LogWarning(ex, "Geçersiz e-posta alıcısı atlanıyor: {Recipient}", recipient);
            }
        }

        if (message.To.Count == 0)
        {
            logger.LogWarning("E-posta gönderimi atlandı (geçerli alıcı yok): {Subject}", subject);
            return;
        }

        message.Subject = subject;
        message.Body = isHtml ? new TextPart("html") { Text = body } : new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort,
            ResolveSecureSocketOptions(settings.SmtpUseStartTls, settings.SmtpPort), ct);

        if (!string.IsNullOrWhiteSpace(settings.SmtpUsername) && !string.IsNullOrEmpty(settings.EncryptedSmtpPassword))
        {
            var plaintextPassword = smtpPasswordProtector.Unprotect(settings.EncryptedSmtpPassword);
            await client.AuthenticateAsync(settings.SmtpUsername, plaintextPassword, ct);
        }

        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation("E-posta gönderildi: {Subject} ({RecipientCount} alıcı)", subject, recipients.Length);
    }

    // StartTLS kapalıyken 465/SMTPS sunucuları için (Ayarlar ekranındaki tooltip'in vaat ettiği gibi)
    // implicit TLS'e geçilir; diğer portlarda Auto'nun sunucu bozuk/yanlış sertifika sunsa bile
    // STARTTLS'i fırsatçı şekilde deneyip patlamasını önlemek için düz metne zorlanır.
    private static SecureSocketOptions ResolveSecureSocketOptions(bool useStartTls, int port)
    {
        if (useStartTls)
            return SecureSocketOptions.StartTls;

        return port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None;
    }
}
