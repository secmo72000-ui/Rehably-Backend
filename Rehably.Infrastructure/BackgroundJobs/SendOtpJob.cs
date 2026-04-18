using Microsoft.Extensions.Logging;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;

namespace Rehably.Infrastructure.BackgroundJobs;

public class SendOtpJob
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<SendOtpJob> _logger;

    public SendOtpJob(
        INotificationService notificationService,
        ILogger<SendOtpJob> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task ExecuteAsync(string contact, string code, int validitySeconds)
    {
        try
        {
            var message = new NotificationMessage
            {
                To = contact,
                Body = $"Your verification code is: {code}. Valid for {validitySeconds} seconds.",
                ChannelProperties = new Dictionary<string, object>
                {
                    { "Subject", "Verification Code" }
                }
            };

            await _notificationService.SendToDefaultChannelAsync("Otp", message);

            _logger.LogInformation("OTP sent successfully to {Contact}", contact);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP to {Contact}", contact);
            throw; // Hangfire will retry
        }
    }
}
