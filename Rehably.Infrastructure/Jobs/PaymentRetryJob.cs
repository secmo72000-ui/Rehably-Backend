using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Infrastructure.Jobs;

public class PaymentRetryJob
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly ISubscriptionLifecycleService _lifecycleService;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<PaymentRetryJob> _logger;

    private const int MaxRetryAttempts = 3;

    public PaymentRetryJob(
        ISubscriptionRepository subscriptionRepository,
        ISubscriptionLifecycleService lifecycleService,
        IBackgroundJobClient backgroundJobClient,
        ILogger<PaymentRetryJob> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _lifecycleService = lifecycleService;
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task RetryPayment(Guid subscriptionId, int attemptNumber)
    {
        var subscription = await _subscriptionRepository.GetWithPackageAsync(subscriptionId);

        if (subscription == null)
        {
            _logger.LogWarning("Subscription {SubscriptionId} not found for payment retry", subscriptionId);
            return;
        }

        if (subscription.Status != SubscriptionStatus.Active)
        {
            _logger.LogInformation("Subscription {SubscriptionId} is not Active (status: {Status}), skipping payment retry",
                subscriptionId, subscription.Status);
            return;
        }

        var idempotencyKey = $"sub_{subscriptionId}_attempt_{attemptNumber}_{subscription.EndDate:yyyyMMdd}";

        _logger.LogInformation("Attempting payment retry {Attempt}/{Max} for subscription {SubscriptionId}. Key: {Key}",
            attemptNumber, MaxRetryAttempts, subscriptionId, idempotencyKey);

        var paymentSucceeded = await AttemptChargeAsync(subscriptionId, idempotencyKey);

        if (paymentSucceeded)
        {
            _logger.LogInformation("Payment succeeded for subscription {SubscriptionId} on attempt {Attempt}",
                subscriptionId, attemptNumber);
            await _lifecycleService.RenewSubscriptionForCycleAsync(subscriptionId);
            return;
        }

        subscription.PaymentRetryCount++;
        _logger.LogWarning("Payment failed for subscription {SubscriptionId} on attempt {Attempt}. RetryCount: {Count}",
            subscriptionId, attemptNumber, subscription.PaymentRetryCount);

        if (attemptNumber < MaxRetryAttempts)
        {
            var retryDelay = TimeSpan.FromDays(2);
            _backgroundJobClient.Schedule<PaymentRetryJob>(
                job => job.RetryPayment(subscriptionId, attemptNumber + 1),
                retryDelay);

            _logger.LogInformation("Scheduled payment retry {Next} in {Days} days for subscription {SubscriptionId}",
                attemptNumber + 1, 2, subscriptionId);
        }
        else
        {
            _backgroundJobClient.Enqueue<SubscriptionSuspensionJob>(
                job => job.ExecuteAsync(subscriptionId));

            _logger.LogWarning("All {Max} payment retries exhausted for subscription {SubscriptionId}. Scheduled suspension.",
                MaxRetryAttempts, subscriptionId);
        }
    }

    private Task<bool> AttemptChargeAsync(Guid subscriptionId, string idempotencyKey)
    {
        // Payment gateway integration point — returns false to trigger retry logic until integrated
        _logger.LogDebug("AttemptChargeAsync called for subscription {SubscriptionId} with key {Key}",
            subscriptionId, idempotencyKey);
        return Task.FromResult(false);
    }
}

public static class PaymentRetryJobRegistration
{
    public static void RegisterPaymentRetryJob(this IServiceCollection services)
    {
        services.AddScoped<PaymentRetryJob>();
    }
}
