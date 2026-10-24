using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Wordrise.Modules.Identity.Core.Email;

/// <summary>Delivers one message. Swappable so tests and local runs never need a mail server.</summary>
internal interface IMailTransport
{
    Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken);
}

/// <summary>Writes the message to the log: local runs read the link from the API console.</summary>
internal sealed class LogMailTransport(ILogger<LogMailTransport> logger) : IMailTransport
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        logger.LogInformation("Email to {To}: {Subject}\n{Body}", to, subject, body);
        return Task.CompletedTask;
    }
}

internal sealed class SmtpMailTransport(IOptions<EmailOptions> options) : IMailTransport
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Smtp.Host) || string.IsNullOrWhiteSpace(settings.From))
        {
            throw new InvalidOperationException("Email:Provider is Smtp but Email:Smtp:Host or Email:From is empty.");
        }

        using var client = new SmtpClient(settings.Smtp.Host, settings.Smtp.Port) { EnableSsl = settings.Smtp.UseSsl };
        if (!string.IsNullOrWhiteSpace(settings.Smtp.User))
        {
            client.Credentials = new NetworkCredential(settings.Smtp.User, settings.Smtp.Password);
        }

        using var message = new MailMessage(settings.From, to, subject, body) { IsBodyHtml = false };
        await client.SendMailAsync(message, cancellationToken);
    }
}
