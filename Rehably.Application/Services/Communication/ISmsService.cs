using Rehably.Application.DTOs.Communication;

namespace Rehably.Application.Services.Communication;

public interface ISmsService
{
    void RegisterProvider(string key, ISmsProvider provider);
    ISmsProvider? GetProvider(string key);
    Task<SmsResult> SendAsync(string providerKey, SmsMessage message, CancellationToken cancellationToken = default);
    Task<SmsResult> SendWithDefaultProviderAsync(SmsMessage message, CancellationToken cancellationToken = default);
}
