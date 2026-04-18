using Microsoft.Extensions.Logging;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;

namespace Rehably.Infrastructure.Services.Communication.Email;

public class MockEmailProvider : IEmailProvider
{
    private readonly ILogger<MockEmailProvider> _logger;
    private readonly string _name;

    public MockEmailProvider(ILogger<MockEmailProvider> logger, string name = "Mock (Development)")
    {
        _logger = logger;
        _name = name;
    }

    public string Name => _name;

    public Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid().ToString();

        var attachmentsInfo = message.Attachments.Count > 0
            ? $"\n├─ Attachments: {message.Attachments.Count}\n" +
              string.Join("\n", message.Attachments.Select(a => $"│  └─ {a.FileName} ({a.ContentType})"))
            : "";

        var content = $"Subject: {message.Subject}\n\n{message.Body}{attachmentsInfo}";

        _logger.LogInformation(
            "📧 MOCK EMAIL SENT\n" +
            "├─ To: {To}\n" +
            "├─ Subject: {Subject}\n" +
            "├─ Body: {Body}\n" +
            "{Attachments}" +
            "└─ Provider: {Provider}",
            message.To,
            message.Subject,
            message.Body.Truncate(100),
            attachmentsInfo,
            Name);

        MockMessageStore.Add(new MockMessage
        {
            Type = "Email",
            To = message.To,
            Content = content
        });

        return Task.FromResult(new EmailResult
        {
            Success = true,
            MessageId = messageId,
            SentAt = DateTime.UtcNow
        });
    }
}

internal static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value.Substring(0, maxLength) + "...";
    }
}
