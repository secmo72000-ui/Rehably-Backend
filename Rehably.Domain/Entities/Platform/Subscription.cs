using Rehably.Domain.Entities.Base;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Platform;

public class Subscription : BaseEntity
{
    public Guid ClinicId { get; set; }
    public Guid PackageId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public string PriceSnapshot { get; set; } = string.Empty;
    public string? PaymentProvider { get; set; }
    public string? ProviderSubscriptionId { get; set; }
    public bool AutoRenew { get; set; } = true;
    public Guid? NextPackageId { get; set; }
    public string? CancelReason { get; set; }
    public int PaymentRetryCount { get; set; } = 0;
    public PaymentType PaymentType { get; set; }

    public Clinic Clinic { get; set; } = null!;
    public Package Package { get; set; } = null!;
    public ICollection<SubscriptionFeatureUsage> FeatureUsage { get; set; } = new List<SubscriptionFeatureUsage>();
    public List<SubscriptionAddOn> AddOns { get; set; } = new();

    #region Domain Methods

    /// <summary>
    /// Checks if the subscription has expired based on end date.
    /// </summary>
    public bool IsExpired() => EndDate <= DateTime.UtcNow;

    /// <summary>
    /// Checks if the subscription can be renewed.
    /// </summary>
    public bool CanRenew() => Status == SubscriptionStatus.Active && CancelledAt == null;

    /// <summary>
    /// Checks if the subscription can be cancelled.
    /// </summary>
    public bool CanCancel() => Status == SubscriptionStatus.Active && CancelledAt == null;

    /// <summary>
    /// Calculates days until expiry.
    /// </summary>
    public int DaysUntilExpiry()
    {
        if (EndDate <= DateTime.UtcNow)
            return 0;
        return (int)(EndDate.Date - DateTime.UtcNow.Date).TotalDays;
    }

    /// <summary>
    /// Checks if the subscription is in grace period (within 7 days after expiry).
    /// </summary>
    public bool IsInGracePeriod()
    {
        if (!IsExpired())
            return false;
        var daysSinceExpiry = (DateTime.UtcNow - EndDate).TotalDays;
        return daysSinceExpiry <= 7;
    }

    /// <summary>
    /// Checks if the subscription is currently in trial period.
    /// </summary>
    public bool IsInTrial() => Status == SubscriptionStatus.Trial && TrialEndsAt.HasValue && TrialEndsAt.Value > DateTime.UtcNow;

    /// <summary>
    /// Checks if the subscription can be upgraded.
    /// </summary>
    public bool CanUpgrade() => Status == SubscriptionStatus.Active || Status == SubscriptionStatus.Trial;

    public bool IsInDunning() => Status == SubscriptionStatus.Active && PaymentRetryCount > 0 && PaymentRetryCount < 3;

    public void Suspend()
    {
        if (Status == SubscriptionStatus.Suspended)
            throw new InvalidOperationException("Subscription is already suspended.");
        if (Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
            throw new InvalidOperationException($"Cannot suspend a {Status} subscription.");

        Status = SubscriptionStatus.Suspended;
        SuspendedAt = DateTime.UtcNow;
    }

    public void RenewForNextCycle()
    {
        if (Status is SubscriptionStatus.Cancelled or SubscriptionStatus.Expired)
            throw new InvalidOperationException($"Cannot renew a {Status} subscription.");

        StartDate = EndDate;
        EndDate = BillingCycle == BillingCycle.Monthly ? EndDate.AddMonths(1) : EndDate.AddYears(1);
        PaymentRetryCount = 0;
        Status = SubscriptionStatus.Active;
        SuspendedAt = null;
    }

    #endregion
}
