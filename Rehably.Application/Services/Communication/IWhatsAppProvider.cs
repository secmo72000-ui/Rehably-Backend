using Rehably.Application.DTOs.Communication;

namespace Rehably.Application.Services.Communication;

public interface IWhatsAppProvider
{
    string Name { get; }
    Task<WhatsAppResult> SendAsync(WhatsAppMessage message, CancellationToken cancellationToken = default);
}
