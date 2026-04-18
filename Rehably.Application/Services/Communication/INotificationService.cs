using Rehably.Application.DTOs.Communication;

namespace Rehably.Application.Services.Communication;

public interface INotificationService
{
    void RegisterChannel(INotificationChannel channel);
    INotificationChannel? GetChannel(NotificationChannelType channelType);
    Task<NotificationResult> SendAsync(NotificationChannelType channelType, NotificationMessage message, CancellationToken cancellationToken = default);
    Task<NotificationResult> SendToDefaultChannelAsync(string action, NotificationMessage message, CancellationToken cancellationToken = default);
}
