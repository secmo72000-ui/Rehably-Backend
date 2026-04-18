using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Clinic;

public record ApproveClinicRequest
{
    /// <summary>For global package approval — the pre-defined package to assign.</summary>
    public Guid? SubscriptionPlanId { get; init; }

    /// <summary>For custom package approval — per-feature limits set by the admin.</summary>
    public List<ApproveFeatureLimit>? FeatureLimits { get; init; }

    /// <summary>Monthly price for custom package (ignored for global packages).</summary>
    public decimal? CustomMonthlyPrice { get; init; }

    /// <summary>Yearly price for custom package (ignored for global packages).</summary>
    public decimal? CustomYearlyPrice { get; init; }

    /// <summary>Override subscription start date. Defaults to UtcNow.</summary>
    public DateTime? SubscriptionStartDate { get; init; }

    /// <summary>Override subscription end date. Calculated from BillingCycle if omitted.</summary>
    public DateTime? SubscriptionEndDate { get; init; }

    /// <summary>How the clinic is paying — Cash (0), Online (1), Free (2).</summary>
    public PaymentType PaymentType { get; init; } = PaymentType.Cash;

    /// <summary>Reference number for externally-collected online payments.</summary>
    public string? PaymentReference { get; init; }

    /// <summary>Admin notes for internal audit trail.</summary>
    public string? Notes { get; init; }
}

public record ApproveFeatureLimit
{
    public Guid FeatureId { get; init; }
    public int? Limit { get; init; }
}
