using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;

namespace Rehably.Infrastructure.Services.Communication.Channels;

public class EmailNotificationChannel : INotificationChannel
{
    private readonly IEmailService _emailService;

    public EmailNotificationChannel(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public NotificationChannelType ChannelType => NotificationChannelType.Email;

    public async Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var emailMessage = new EmailMessage
        {
            To = message.To,
            Subject = GetSubject(message),
            Body = message.Body,
            IsHtml = false
        };

        var result = await _emailService.SendWithDefaultProviderAsync(emailMessage, cancellationToken);

        if (result.Success)
        {
            return NotificationResult.Ok(result.MessageId);
        }

        return NotificationResult.Fail(result.ErrorMessage ?? "Failed to send email");
    }

    private static string GetSubject(NotificationMessage message)
    {
        if (message.ChannelProperties.TryGetValue("Subject", out var subject))
        {
            return subject.ToString() ?? string.Empty;
        }

        return "Rehably Notification";
    }
}
