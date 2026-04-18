using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Services.Communication;

namespace Rehably.Infrastructure.BackgroundJobs;

public class SendWelcomeEmailJob
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SendWelcomeEmailJob> _logger;

    public SendWelcomeEmailJob(
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<SendWelcomeEmailJob> logger)
    {
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task ExecuteAsync(string email, string token, string clinicName, string userName)
    {
        try
        {
            var frontendUrl = _configuration["AppSettings:FrontendUrl"] ?? "http://localhost:3000";
            var resetLink = $"{frontendUrl}/set-password?token={token}&email={Uri.EscapeDataString(email)}";

            var body = $@"Welcome to {clinicName}, {userName}!

Your account has been created successfully.

Please click the link below to set your password and access your account:

{resetLink}

This link will expire in 24 hours.

If you didn't request this account, please contact support immediately.";

            var message = new EmailMessage
            {
                To = email,
                Subject = $"Welcome to {clinicName} - Set Your Password",
                Body = body,
                IsHtml = false
            };

            await _emailService.SendWithDefaultProviderAsync(message);

            _logger.LogInformation("Welcome email sent successfully to {Email} for {ClinicName}", email, clinicName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email} for {ClinicName}", email, clinicName);
            throw; // Hangfire will retry
        }
    }
}
