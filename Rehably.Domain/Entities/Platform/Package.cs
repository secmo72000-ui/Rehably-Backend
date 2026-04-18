using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Platform;

public class Package
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public decimal CalculatedMonthlyPrice { get; set; }
    public decimal CalculatedYearlyPrice { get; set; }
    public bool IsPublic { get; set; } = true;
    public bool IsCustom { get; set; } = false;
    public Guid? ForClinicId { get; set; }
    public PackageStatus Status { get; set; }
    public PackageTier Tier { get; set; } = PackageTier.Basic;
    public bool IsPopular { get; set; } = false;
    public int DisplayOrder { get; set; }
    public int TrialDays { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public ICollection<PackageFeature> Features { get; set; } = new List<PackageFeature>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();

    #region Domain Methods

    /// <summary>
    /// Checks if the package has a specific feature by feature ID.
    /// </summary>
    public bool HasFeature(Guid featureId) => Features.Any(f => f.FeatureId == featureId && f.IsIncluded);

    /// <summary>
    /// Gets the limit for a specific feature.
    /// </summary>
    public int? GetFeatureLimit(Guid featureId)
    {
        var feature = Features.FirstOrDefault(f => f.FeatureId == featureId);
        return feature?.Limit;
    }

    /// <summary>
    /// Calculates the price based on billing cycle.
    /// </summary>
    public decimal GetPrice(BillingCycle billingCycle) => billingCycle == BillingCycle.Yearly ? YearlyPrice : MonthlyPrice;

    public decimal CalculatePrice(BillingCycle billingCycle)
    {
        return billingCycle switch
        {
            BillingCycle.Monthly => CalculatedMonthlyPrice,
            BillingCycle.Yearly => CalculatedYearlyPrice,
            _ => CalculatedMonthlyPrice
        };
    }

    /// <summary>
    /// Validates the package.
    /// </summary>
    public bool IsValid()
    {
        return MonthlyPrice >= 0 &&
               YearlyPrice >= 0 &&
               TrialDays >= 0;
    }

    #endregion
}
