using Rehably.Application.DTOs.Communication;

namespace Rehably.Application.Services.Communication;

public interface IWhatsAppService
{
    void RegisterProvider(string key, IWhatsAppProvider provider);
    IWhatsAppProvider? GetProvider(string key);
    Task<WhatsAppResult> SendAsync(string providerKey, WhatsAppMessage message, CancellationToken cancellationToken = default);
    Task<WhatsAppResult> SendWithDefaultProviderAsync(WhatsAppMessage message, CancellationToken cancellationToken = default);
}
