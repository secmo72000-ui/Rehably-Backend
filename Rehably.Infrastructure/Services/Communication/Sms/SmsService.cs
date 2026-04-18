using Microsoft.Extensions.Options;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;
using Rehably.Infrastructure.Services.Communication.Sms;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Services.Communication.Sms;

public class SmsService : ISmsService
{
    private readonly SmsSettings _settings;
    private readonly Dictionary<string, ISmsProvider> _providers;

    public SmsService(IOptions<SmsSettings> settings, IEnumerable<ISmsProvider> providers)
    {
        _settings = settings.Value;
        _providers = new Dictionary<string, ISmsProvider>();

        foreach (var provider in providers)
        {
            RegisterProvider(provider.Name, provider);
        }
    }

    public void RegisterProvider(string key, ISmsProvider provider)
    {
        _providers[key] = provider;
    }

    public ISmsProvider? GetProvider(string key)
    {
        return _providers.GetValueOrDefault(key);
    }

    public Task<SmsResult> SendAsync(string providerKey, SmsMessage message, CancellationToken cancellationToken = default)
    {
        var provider = GetProvider(providerKey);
        if (provider == null)
        {
            return Task.FromResult(new SmsResult
            {
                Success = false,
                ErrorMessage = $"Provider '{providerKey}' not found."
            });
        }

        return provider.SendAsync(message, cancellationToken);
    }

    public Task<SmsResult> SendWithDefaultProviderAsync(SmsMessage message, CancellationToken cancellationToken = default)
    {
        return SendAsync(_settings.DefaultProvider, message, cancellationToken);
    }
}
