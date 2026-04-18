namespace Rehably.Application.Services.Platform;

public interface ISubscriptionNotificationService
{
    Task SendSubscriptionCreatedEmailAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task SendSubscriptionCancelledEmailAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task SendSubscriptionRenewedEmailAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task SendSubscriptionUpgradedEmailAsync(Guid subscriptionId, string oldPackageName, CancellationToken cancellationToken = default);
    Task SendSubscriptionExpiringEmailAsync(Guid subscriptionId, int daysUntilExpiry, CancellationToken cancellationToken = default);
    Task SendPaymentFailedEmailAsync(Guid subscriptionId, CancellationToken cancellationToken = default);
    Task SendTrialEndingReminderAsync(Guid subscriptionId, int daysRemaining, CancellationToken cancellationToken = default);
}
