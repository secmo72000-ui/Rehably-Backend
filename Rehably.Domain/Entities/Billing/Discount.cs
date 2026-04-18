using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Billing;

public class Discount
{
    public Guid Id { get; set; }
    public Guid ClinicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public string? Code { get; set; }                          // null = not a promo code
    public DiscountType Type { get; set; }
    public decimal Value { get; set; }                         // percent or EGP
    public DiscountAppliesTo AppliesTo { get; set; } = DiscountAppliesTo.Any;
    public DiscountApplicationMethod ApplicationMethod { get; set; } = DiscountApplicationMethod.Manual;
    public string? AutoCondition { get; set; }                 // e.g. "first_appointment", "day:tuesday"
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? MaxUsageTotal { get; set; }
    public int? MaxUsagePerPatient { get; set; }
    public int UsageCount { get; set; } = 0;
    public decimal TotalValueGiven { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public SessionPackageOffer? PackageOffer { get; set; }
    public ICollection<DiscountUsage> Usages { get; set; } = new List<DiscountUsage>();
}
