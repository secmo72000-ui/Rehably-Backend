using Rehably.Application.DTOs.Communication;

namespace Rehably.Application.Services.Communication;

public interface IEmailService
{
    Task<EmailResult> SendAsync(string providerKey, EmailMessage message, CancellationToken cancellationToken = default);
    Task<EmailResult> SendWithDefaultProviderAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
