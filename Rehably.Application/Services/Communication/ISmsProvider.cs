using Rehably.Application.DTOs.Communication;

namespace Rehably.Application.Services.Communication;

public interface ISmsProvider
{
    string Name { get; }
    Task<SmsResult> SendAsync(SmsMessage message, CancellationToken cancellationToken = default);
}
