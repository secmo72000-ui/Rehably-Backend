using Microsoft.Extensions.Options;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;
using Rehably.Infrastructure.Services.Communication.WhatsApp;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Services.Communication.WhatsApp;

public class WhatsAppService : IWhatsAppService
{
    private readonly WhatsAppSettings _settings;
    private readonly Dictionary<string, IWhatsAppProvider> _providers;

    public WhatsAppService(IOptions<WhatsAppSettings> settings, IEnumerable<IWhatsAppProvider> providers)
    {
        _settings = settings.Value;
        _providers = new Dictionary<string, IWhatsAppProvider>();

        foreach (var provider in providers)
        {
            RegisterProvider(provider.Name, provider);
        }
    }

    public void RegisterProvider(string key, IWhatsAppProvider provider)
    {
        _providers[key] = provider;
    }

    public IWhatsAppProvider? GetProvider(string key)
    {
        return _providers.GetValueOrDefault(key);
    }

    public Task<WhatsAppResult> SendAsync(string providerKey, WhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(providerKey);
        if (provider == null)
        {
            return Task.FromResult(new WhatsAppResult
            {
                Success = false,
                ErrorMessage = $"Provider '{providerKey}' not found."
            });
        }

        return provider.SendAsync(message, cancellationToken);
    }

    public Task<WhatsAppResult> SendWithDefaultProviderAsync(WhatsAppMessage message, CancellationToken cancellationToken = default)
    {
        return SendAsync(_settings.DefaultProvider, message, cancellationToken);
    }
}
