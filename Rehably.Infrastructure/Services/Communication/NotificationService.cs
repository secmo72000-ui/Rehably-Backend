using Microsoft.Extensions.Options;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;
using Rehably.Infrastructure.Settings;

namespace Rehably.Infrastructure.Services.Communication;

public class NotificationService : INotificationService
{
    private readonly Dictionary<NotificationChannelType, INotificationChannel> _channels;
    private readonly NotificationSettings _settings;

    public NotificationService(IOptions<NotificationSettings> settings, IEnumerable<INotificationChannel> channels)
    {
        _settings = settings.Value;
        _channels = channels.ToDictionary(c => c.ChannelType);
    }

    public void RegisterChannel(INotificationChannel channel)
    {
        _channels[channel.ChannelType] = channel;
    }

    public INotificationChannel? GetChannel(NotificationChannelType channelType)
    {
        return _channels.GetValueOrDefault(channelType);
    }

    public async Task<NotificationResult> SendAsync(NotificationChannelType channelType, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var channel = GetChannel(channelType);
        if (channel is null)
        {
            return NotificationResult.Fail($"No channel registered for type: {channelType}");
        }

        return await channel.SendAsync(message, cancellationToken);
    }

    public async Task<NotificationResult> SendToDefaultChannelAsync(string action, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (!_settings.DefaultChannels.TryGetValue(action, out var channelName))
        {
            return NotificationResult.Fail($"No default channel configured for action: {action}");
        }

        if (!Enum.TryParse<NotificationChannelType>(channelName, ignoreCase: true, out var channelType))
        {
            return NotificationResult.Fail($"Invalid channel type in configuration: {channelName}");
        }

        return await SendAsync(channelType, message, cancellationToken);
    }
}
