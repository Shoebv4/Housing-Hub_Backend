using HousingHubBackend.Services.Interfaces;
using HousingHubBackend.Data;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HousingHubBackend.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private static readonly TimeSpan SmtpTimeout = TimeSpan.FromSeconds(30);

        private readonly HousingHubDBContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(HousingHubDBContext context, IConfiguration configuration, ILogger<NotificationService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        // Email delivery is best effort: failures are logged so the business operation can still succeed.
        public async Task SendNotificationAsync(string to, string subject, string message)
        {
            try
            {
                await SendEmailAsync(to, subject, message);
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError(ex, "SMTP rejected the email '{Subject}' for {Recipient}. StatusCode: {StatusCode}, ErrorCode: {ErrorCode}.",
                    subject, to, ex.StatusCode, ex.ErrorCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email '{Subject}' to {Recipient}.", subject, to);
            }
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            var smtpSection = _configuration.GetSection("Smtp");
            var host = smtpSection["Host"];
            var portString = smtpSection["Port"];
            var username = smtpSection["Username"];
            var password = smtpSection["Password"];
            var from = smtpSection["From"] ?? username;
            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("SMTP host configuration is missing.");
            if (string.IsNullOrWhiteSpace(portString))
                throw new InvalidOperationException("SMTP port configuration is missing.");
            if (!int.TryParse(portString, out var port))
                throw new InvalidOperationException($"SMTP port configuration '{portString}' is not a valid number.");
            if (string.IsNullOrWhiteSpace(username))
                throw new InvalidOperationException("SMTP username configuration is missing.");
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("SMTP password configuration is missing.");
            if (string.IsNullOrWhiteSpace(from))
                throw new InvalidOperationException("SMTP from address configuration is missing.");

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = body };

            using var timeout = new CancellationTokenSource(SmtpTimeout);
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls, timeout.Token);
            await smtp.AuthenticateAsync(username, password, timeout.Token);
            await smtp.SendAsync(email, timeout.Token);
            await smtp.DisconnectAsync(true, timeout.Token);
        }
    }
}