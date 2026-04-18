using Microsoft.Extensions.Logging;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;

namespace Rehably.Infrastructure.Services.Communication.WhatsApp;

public class MockWhatsAppProvider : IWhatsAppProvider
{
    private readonly ILogger<MockWhatsAppProvider> _logger;
    private readonly string _name;

    public MockWhatsAppProvider(ILogger<MockWhatsAppProvider> logger, string name = "Mock (Development)")
    {
        _logger = logger;
        _name = name;
    }

    public string Name => _name;

    public Task<WhatsAppResult> SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid().ToString();

        var mediaInfo = message.Media.Count > 0
            ? $"\n├─ Media: {message.Media.Count}\n" +
              string.Join("\n", message.Media.Select(m => $"│  └─ {m.ContentType}: {m.Url}"))
            : "";

        var content = $"{message.Body}{mediaInfo}";

        _logger.LogInformation(
            "💬 MOCK WHATSAPP SENT\n" +
            "├─ To: {To}\n" +
            "├─ Message: {Body}\n" +
            "{Media}" +
            "└─ Provider: {Provider}",
            message.To,
            message.Body,
            mediaInfo,
            Name);

        MockMessageStore.Add(new MockMessage
        {
            Type = "WhatsApp",
            To = message.To,
            Content = content
        });

        return Task.FromResult(new WhatsAppResult
        {
            Success = true,
            MessageId = messageId,
            SentAt = DateTime.UtcNow
        });
    }
}
