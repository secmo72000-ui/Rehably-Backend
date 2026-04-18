using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Subscription;

public record CreateSubscriptionRequestDto
{
    public Guid ClinicId { get; init; }
    public Guid PackageId { get; init; }
    public bool AutoRenew { get; init; } = true;
    public string? PaymentProvider { get; init; }
    public string? CouponCode { get; init; }

    /// <summary>Payment type: Cash (admin manual), Online, or Free. Defaults to Cash.</summary>
    public PaymentType PaymentType { get; init; } = PaymentType.Cash;

    /// <summary>Billing cycle used to calculate end date when EndDate is not provided.</summary>
    public BillingCycle BillingCycle { get; init; } = BillingCycle.Monthly;

    /// <summary>Admin override: subscription start date. Defaults to UtcNow.</summary>
    public DateTime? StartDate { get; init; }

    /// <summary>Admin override: subscription end date. Calculated from BillingCycle if omitted.</summary>
    public DateTime? EndDate { get; init; }
}
