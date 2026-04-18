using Rehably.Application.DTOs.Communication;

namespace Rehably.Application.Services.Communication;

public interface IEmailProvider
{
    string Name { get; }
    Task<EmailResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}
