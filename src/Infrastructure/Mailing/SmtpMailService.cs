using Retailer.Application.Common.Mailing;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Retailer.Infrastructure.Mailing;

public class SmtpMailService : IMailService
{
    private readonly MailSettings _settings;
    private readonly ILogger<SmtpMailService> _logger;

    public SmtpMailService(IOptions<MailSettings> settings, ILogger<SmtpMailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendAsync(MailRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(_settings.DisplayName, request.From ?? _settings.From));

            foreach (string address in request.To)
                email.To.Add(MailboxAddress.Parse(address));

            if (!string.IsNullOrEmpty(request.ReplyTo))
                email.ReplyTo.Add(new MailboxAddress(request.ReplyToName, request.ReplyTo));

            if (request.Bcc != null)
            {
                foreach (string address in request.Bcc.Where(bccValue => !string.IsNullOrWhiteSpace(bccValue)))
                    email.Bcc.Add(MailboxAddress.Parse(address.Trim()));
            }

            if (request.Cc != null)
            {
                foreach (string? address in request.Cc.Where(ccValue => !string.IsNullOrWhiteSpace(ccValue)))
                    email.Cc.Add(MailboxAddress.Parse(address.Trim()));
            }

            if (request.Headers != null)
            {
                foreach (var header in request.Headers)
                    email.Headers.Add(header.Key, header.Value);
            }

            var builder = new BodyBuilder();
            email.Sender = new MailboxAddress(request.DisplayName ?? _settings.DisplayName, request.From ?? _settings.From);
            email.Subject = request.Subject;
            builder.HtmlBody = request.Body;

            if (request.AttachmentData != null)
            {
                foreach (var attachmentInfo in request.AttachmentData)
                    builder.Attachments.Add(attachmentInfo.Key, attachmentInfo.Value);
            }

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.Auto, cancellationToken);
            await smtp.AuthenticateAsync(_settings.UserName, _settings.Password, cancellationToken);
            await smtp.SendAsync(email, cancellationToken);
            await smtp.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }
    }
}