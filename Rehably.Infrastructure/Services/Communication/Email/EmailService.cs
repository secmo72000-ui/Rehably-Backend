using Microsoft.Extensions.Options;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Services.Communication.Email;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly Dictionary<string, IEmailProvider> _providers;

    public EmailService(IOptions<EmailSettings> settings, IEnumerable<IEmailProvider> providers)
    {
        _settings = settings.Value;
        _providers = new Dictionary<string, IEmailProvider>();

        foreach (var provider in providers)
        {
            _providers[provider.Name] = provider;
        }
    }

    public Task<EmailResult> SendAsync(string providerKey, EmailMessage message, CancellationToken cancellationToken = default)
    {
        var provider = _providers.GetValueOrDefault(providerKey);
        if (provider == null)
        {
            return Task.FromResult(new EmailResult
            {
                Success = false,
                ErrorMessage = $"Provider '{providerKey}' not found."
            });
        }

        return provider.SendAsync(message, cancellationToken);
    }

    public Task<EmailResult> SendWithDefaultProviderAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        return SendAsync(_settings.DefaultProvider, message, cancellationToken);
    }
}
