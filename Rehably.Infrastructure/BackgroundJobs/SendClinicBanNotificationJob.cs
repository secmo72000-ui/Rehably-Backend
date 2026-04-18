using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;

namespace Rehably.Infrastructure.BackgroundJobs;

public class SendClinicBanNotificationJob
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendClinicBanNotificationJob> _logger;

    public SendClinicBanNotificationJob(
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<SendClinicBanNotificationJob> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(string email, string clinicName, string banReason)
    {
        try
        {
            var supportEmail = _configuration["AppSettings:SupportEmail"] ?? "support@rehably.com";
            var appName = _configuration["AppSettings:AppName"] ?? "Rehably";

            var body = $@"Dear {clinicName} Administrator,

We regret to inform you that your clinic account has been suspended.

Reason for suspension:
{banReason}

What this means:
- All users associated with your clinic have been logged out
- Access to the platform has been temporarily disabled
- Your data remains secure and intact

If you believe this was done in error or would like to appeal this decision, please contact our support team at {supportEmail}.

Best regards,
The {appName} Team";

            var message = new EmailMessage
            {
                To = email,
                Subject = $"[{appName}] Account Suspension Notice - {clinicName}",
                Body = body,
                IsHtml = false
            };

            await _emailService.SendWithDefaultProviderAsync(message);

            _logger.LogInformation("Ban notification email sent successfully to {Email} for clinic {ClinicName}", email, clinicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send ban notification email to {Email} for clinic {ClinicName}", email, clinicName);
            throw; // Hangfire will retry
        }
    }
}
