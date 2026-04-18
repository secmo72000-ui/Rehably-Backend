using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;

namespace Rehably.Infrastructure.Services.Communication.Email;

public class SmtpEmailProvider : IEmailProvider
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly bool _useSsl;
    private readonly string _defaultFromEmail;
    private readonly string _defaultFromName;

    public SmtpEmailProvider(string host, int port, string username, string password, bool useSsl, string defaultFromEmail, string defaultFromName)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _useSsl = useSsl;
        _defaultFromEmail = defaultFromEmail;
        _defaultFromName = defaultFromName;
    }

    public string Name => "SMTP";

    public async Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient();
            var secureOptions = _useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(_host, _port, secureOptions, cancellationToken);
            await client.AuthenticateAsync(_username, _password, cancellationToken);

            var mimeMessage = new MimeMessage();
            mimeMessage.From.Add(new MailboxAddress(_defaultFromName, _defaultFromEmail));
            mimeMessage.To.Add(new MailboxAddress("", message.To));
            mimeMessage.Subject = message.Subject;

            var bodyBuilder = new BodyBuilder();
            if (message.IsHtml)
            {
                bodyBuilder.HtmlBody = message.Body;
            }
            else
            {
                bodyBuilder.TextBody = message.Body;
            }

            foreach (var attachment in message.Attachments)
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            var response = await client.SendAsync(mimeMessage, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            return new EmailResult
            {
                Success = true,
                MessageId = response
            };
        }
        catch (Exception ex)
        {
            return new EmailResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
