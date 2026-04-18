namespace Rehably.Domain.Entities.Platform;

public enum NotificationType
{
    TrialEnding = 1,
    TrialEnded = 2,
    UsageWarning80 = 3,
    UsageWarning100 = 4,
    InvoiceGenerated = 5,
    PaymentSucceeded = 6,
    PaymentFailed = 7,
    SubscriptionRenewed = 8,
    SubscriptionCancelled = 9,
    SubscriptionExpired = 10,
    UpgradeScheduled = 11
}
