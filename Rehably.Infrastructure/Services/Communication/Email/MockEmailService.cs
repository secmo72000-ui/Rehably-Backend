using Microsoft.Extensions.Options;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Services.Communication.Email;

public class MockEmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public MockEmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public Task<EmailResult> SendAsync(string providerKey, EmailMessage message, CancellationToken cancellationToken = default)
    {
        var mockProvider = new MockEmailProvider(
            new Microsoft.Extensions.Logging.Abstractions.NullLogger<MockEmailProvider>(),
            _settings.DefaultProvider);

        return mockProvider.SendAsync(message, cancellationToken);
    }

    public Task<EmailResult> SendWithDefaultProviderAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        return SendAsync(_settings.DefaultProvider, message, cancellationToken);
    }
}
