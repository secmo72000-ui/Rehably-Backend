using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;

namespace Rehably.Infrastructure.Services.Communication.Email;

public class MailtrapEmailProvider : IEmailProvider
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _defaultFromEmail;
    private readonly string _defaultFromName;

    public MailtrapEmailProvider(string host, int port, string username, string password, string defaultFromEmail, string defaultFromName)
    {
        _host = host;
        _port = port;
        _username = username;
        _password = password;
        _defaultFromEmail = defaultFromEmail;
        _defaultFromName = defaultFromName;
    }

    public string Name => "Mailtrap";

    public async Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_host, _port, SecureSocketOptions.StartTls, cancellationToken);
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
