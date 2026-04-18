using Microsoft.Extensions.Logging;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;

namespace Rehably.Infrastructure.Services.Communication.Sms;

public class MockSmsProvider : ISmsProvider
{
    private readonly ILogger<MockSmsProvider> _logger;
    private readonly string _name;

    public MockSmsProvider(ILogger<MockSmsProvider> logger, string name = "Mock (Development)")
    {
        _logger = logger;
        _name = name;
    }

    public string Name => _name;

    public Task<SmsResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        var messageId = Guid.NewGuid().ToString();

        _logger.LogInformation(
            "📱 MOCK SMS SENT\n" +
            "├─ To: {To}\n" +
            "├─ Message: {Body}\n" +
            "└─ Provider: {Provider}",
            message.To,
            message.Body,
            Name);

        MockMessageStore.Add(new MockMessage
        {
            Type = "SMS",
            To = message.To,
            Content = message.Body
        });

        return Task.FromResult(new SmsResult
        {
            Success = true,
            MessageId = messageId,
            SentAt = DateTime.UtcNow
        });
    }
}
