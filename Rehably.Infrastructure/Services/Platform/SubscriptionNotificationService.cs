using Microsoft.Extensions.Logging;
using Rehably.Application.DTOs.Communication;
using Rehably.Application.Interfaces;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Communication;
using Rehably.Application.Services.Platform;

namespace Rehably.Infrastructure.Services.Platform;

public class SubscriptionNotificationService : ISubscriptionNotificationService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<SubscriptionNotificationService> _logger;
    private readonly IClock _clock;

    public SubscriptionNotificationService(
        ISubscriptionRepository subscriptionRepository,
        IClinicRepository clinicRepository,
        IEmailService emailService,
        ILogger<SubscriptionNotificationService> logger,
        IClock clock)
    {
        _subscriptionRepository = subscriptionRepository;
        _clinicRepository = clinicRepository;
        _emailService = emailService;
        _logger = logger;
        _clock = clock;
    }

    public async Task SendSubscriptionCreatedEmailAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for email notification", subscriptionId);
            return;
        }

        var clinic = await _clinicRepository.GetByIdAsync(subscription.ClinicId);

        if (clinic == null || string.IsNullOrEmpty(clinic.Email))
        {
            _logger.LogWarning("Clinic or email not found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        var body = SubscriptionEmailTemplates.SubscriptionCreated(
            clinic.Name,
            subscription.Package!.Name,
            subscription.StartDate,
            subscription.EndDate,
            subscription.TrialEndsAt
        );

        var message = new EmailMessage
        {
            To = clinic.Email,
            Subject = "Your Subscription Has Been Activated",
            Body = body,
            IsHtml = true
        };

        try
        {
            await _emailService.SendWithDefaultProviderAsync(message, cancellationToken);
            _logger.LogInformation("Subscription created email sent to {Email}", clinic.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription created email to {Email}", clinic.Email);
        }
    }

    public async Task SendSubscriptionCancelledEmailAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for email notification", subscriptionId);
            return;
        }

        var clinic = await _clinicRepository.GetByIdAsync(subscription.ClinicId);

        if (clinic == null || string.IsNullOrEmpty(clinic.Email))
        {
            _logger.LogWarning("Clinic or email not found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        var body = SubscriptionEmailTemplates.SubscriptionCancelled(
            clinic.Name,
            subscription.Package!.Name,
            subscription.CancelledAt ?? _clock.UtcNow,
            subscription.CancelReason
        );

        var message = new EmailMessage
        {
            To = clinic.Email,
            Subject = "Your Subscription Has Been Cancelled",
            Body = body,
            IsHtml = true
        };

        try
        {
            await _emailService.SendWithDefaultProviderAsync(message, cancellationToken);
            _logger.LogInformation("Subscription cancelled email sent to {Email}", clinic.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription cancelled email to {Email}", clinic.Email);
        }
    }

    public async Task SendSubscriptionRenewedEmailAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for email notification", subscriptionId);
            return;
        }

        var clinic = await _clinicRepository.GetByIdAsync(subscription.ClinicId);

        if (clinic == null || string.IsNullOrEmpty(clinic.Email))
        {
            _logger.LogWarning("Clinic or email not found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        var body = SubscriptionEmailTemplates.SubscriptionRenewed(
            clinic.Name,
            subscription.Package!.Name,
            subscription.StartDate,
            subscription.EndDate
        );

        var message = new EmailMessage
        {
            To = clinic.Email,
            Subject = "Your Subscription Has Been Renewed",
            Body = body,
            IsHtml = true
        };

        try
        {
            await _emailService.SendWithDefaultProviderAsync(message, cancellationToken);
            _logger.LogInformation("Subscription renewed email sent to {Email}", clinic.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription renewed email to {Email}", clinic.Email);
        }
    }

    public async Task SendSubscriptionUpgradedEmailAsync(Guid subscriptionId, string oldPackageName, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for email notification", subscriptionId);
            return;
        }

        var clinic = await _clinicRepository.GetByIdAsync(subscription.ClinicId);

        if (clinic == null || string.IsNullOrEmpty(clinic.Email))
        {
            _logger.LogWarning("Clinic or email not found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        var body = SubscriptionEmailTemplates.SubscriptionUpgraded(
            clinic.Name,
            oldPackageName,
            subscription.Package!.Name,
            subscription.UpdatedAt ?? _clock.UtcNow
        );

        var message = new EmailMessage
        {
            To = clinic.Email,
            Subject = "Your Subscription Has Been Upgraded",
            Body = body,
            IsHtml = true
        };

        try
        {
            await _emailService.SendWithDefaultProviderAsync(message, cancellationToken);
            _logger.LogInformation("Subscription upgraded email sent to {Email}", clinic.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription upgraded email to {Email}", clinic.Email);
        }
    }

    public async Task SendSubscriptionExpiringEmailAsync(Guid subscriptionId, int daysUntilExpiry, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for email notification", subscriptionId);
            return;
        }

        var clinic = await _clinicRepository.GetByIdAsync(subscription.ClinicId);

        if (clinic == null || string.IsNullOrEmpty(clinic.Email))
        {
            _logger.LogWarning("Clinic or email not found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        var body = SubscriptionEmailTemplates.SubscriptionExpiring(
            clinic.Name,
            subscription.Package!.Name,
            subscription.EndDate,
            daysUntilExpiry
        );

        var message = new EmailMessage
        {
            To = clinic.Email,
            Subject = $"Your Subscription Expires in {daysUntilExpiry} Day(s)",
            Body = body,
            IsHtml = true
        };

        try
        {
            await _emailService.SendWithDefaultProviderAsync(message, cancellationToken);
            _logger.LogInformation("Subscription expiring email sent to {Email}", clinic.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription expiring email to {Email}", clinic.Email);
        }
    }

    public async Task SendPaymentFailedEmailAsync(Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for email notification", subscriptionId);
            return;
        }

        var clinic = await _clinicRepository.GetByIdAsync(subscription.ClinicId);

        if (clinic == null || string.IsNullOrEmpty(clinic.Email))
        {
            _logger.LogWarning("Clinic or email not found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        var body = SubscriptionEmailTemplates.PaymentFailed(
            clinic.Name,
            subscription.Package!.Name,
            _clock.UtcNow,
            null
        );

        var message = new EmailMessage
        {
            To = clinic.Email,
            Subject = "Payment Failed - Action Required",
            Body = body,
            IsHtml = true
        };

        try
        {
            await _emailService.SendWithDefaultProviderAsync(message, cancellationToken);
            _logger.LogInformation("Payment failed email sent to {Email}", clinic.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment failed email to {Email}", clinic.Email);
        }
    }

    public async Task SendTrialEndingReminderAsync(Guid subscriptionId, int daysRemaining, CancellationToken cancellationToken = default)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);
        if (subscription == null) return;

        var clinic = await _clinicRepository.GetByIdAsync(subscription.ClinicId);
        if (clinic?.Email == null) return;

        var body = $"<p>Hello {clinic.Name},</p>" +
                   $"<p>Your trial for <strong>{subscription.Package?.Name}</strong> ends in <strong>{daysRemaining} day(s)</strong>.</p>" +
                   "<p>Please upgrade to continue using all features.</p>";

        var message = new EmailMessage
        {
            To = clinic.Email,
            Subject = $"Trial Ending in {daysRemaining} Day(s)",
            Body = body,
            IsHtml = true
        };

        try
        {
            await _emailService.SendWithDefaultProviderAsync(message, cancellationToken);
            _logger.LogInformation("Trial ending reminder sent to {Email}", clinic.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send trial ending reminder to {Email}", clinic.Email);
        }
    }
}
