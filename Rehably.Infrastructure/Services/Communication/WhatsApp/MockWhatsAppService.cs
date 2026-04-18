using Microsoft.Extensions.Options;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Services.Communication.WhatsApp;

public class MockWhatsAppService : IWhatsAppService
{
    private readonly WhatsAppSettings _settings;

    public MockWhatsAppService(IOptions<WhatsAppSettings> settings)
    {
        _settings = settings.Value;
    }

    public void RegisterProvider(string key, IWhatsAppProvider provider)
    {
        throw new NotImplementedException("Mock service does not support dynamic provider registration.");
    }

    public IWhatsAppProvider? GetProvider(string key)
    {
        throw new NotImplementedException("Mock service does not support provider lookup.");
    }

    public Task<WhatsAppResult> SendAsync(string providerKey, WhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        var mockProvider = new MockWhatsAppProvider(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<MockWhatsAppProvider>(),
            _settings.DefaultProvider);

        return mockProvider.SendAsync(message, cancellationToken);
    }

    public Task<WhatsAppResult> SendWithDefaultProviderAsync(WhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        return SendAsync(_settings.DefaultProvider, message, cancellationToken);
    }
}
