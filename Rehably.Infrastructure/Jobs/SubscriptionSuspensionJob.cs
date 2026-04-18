using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Jobs;

public class SubscriptionSuspensionJob
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionLifecycleService _lifecycleService;
    private readonly ILogger<SubscriptionSuspensionJob> _logger;

    private const int MaxRetryAttempts = 3;

    public SubscriptionSuspensionJob(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionLifecycleService lifecycleService,
        ILogger<SubscriptionSuspensionJob> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _lifecycleService = lifecycleService;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for suspension", subscriptionId);
            return;
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            _logger.LogInformation("Subscription {SubscriptionId} is not Active (status: {Status}), skipping suspension",
                subscriptionId, subscription.Status);
            return;
        }

        if (subscription.PaymentRetryCount < MaxRetryAttempts)
        {
            _logger.LogInformation("Subscription {SubscriptionId} has only {Count} retries (< {Max}), skipping suspension",
                subscriptionId, subscription.PaymentRetryCount, MaxRetryAttempts);
            return;
        }

        var result = await _lifecycleService.SuspendClinicAsync(subscription.ClinicId, "All payment retries exhausted");

        if (result.IsSuccess)
        {
            _logger.LogInformation("Clinic {ClinicId} suspended after exhausting payment retries for subscription {SubscriptionId}",
                subscription.ClinicId, subscriptionId);
        }
        else
        {
            _logger.LogError("Failed to suspend clinic {ClinicId} for subscription {SubscriptionId}: {Error}",
                subscription.ClinicId, subscriptionId, result.Error);
        }
    }
}

public static class SubscriptionSuspensionJobRegistration
{
    public static void RegisterSubscriptionSuspensionJob(this IServiceCollection services)
    {
        services.AddScoped<SubscriptionSuspensionJob>();
    }
}
