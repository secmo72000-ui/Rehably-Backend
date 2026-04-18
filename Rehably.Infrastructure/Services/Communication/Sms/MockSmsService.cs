using Microsoft.Extensions.Options;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Services.Communication.Sms;

public class MockSmsService : ISmsService
{
    private readonly SmsSettings _settings;

    public MockSmsService(IOptions<SmsSettings> settings)
    {
        _settings = settings.Value;
    }

    public void RegisterProvider(string key, ISmsProvider provider)
    {
        throw new NotImplementedException("Mock service does not support dynamic provider registration.");
    }

    public ISmsProvider? GetProvider(string key)
    {
        throw new NotImplementedException("Mock service does not support provider lookup.");
    }

    public Task<SmsResult> SendAsync(string providerKey, SmsMessage message, CancellationToken cancellationToken = default)
    {
        var mockProvider = new MockSmsProvider(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<MockSmsProvider>(),
            _settings.DefaultProvider);

        return mockProvider.SendAsync(message, cancellationToken);
    }

    public Task<SmsResult> SendWithDefaultProviderAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        return SendAsync(_settings.DefaultProvider, message, cancellationToken);
    }
}
