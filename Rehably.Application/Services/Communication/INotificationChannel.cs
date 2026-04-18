using Rehably.Application.DTOs.Communication;

namespace Rehably.Application.Services.Communication;

public interface INotificationChannel
{
    NotificationChannelType ChannelType { get; }
    Task<NotificationResult> SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
