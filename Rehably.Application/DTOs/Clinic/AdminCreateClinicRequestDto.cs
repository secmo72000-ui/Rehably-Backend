using Rehably.Application.DTOs.Package;
using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Clinic;

public record AdminCreateClinicRequestDto
{
    public string ClinicName { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string OwnerFirstName { get; init; } = string.Empty;
    public string OwnerLastName { get; init; } = string.Empty;
    public string OwnerEmail { get; init; } = string.Empty;
    public string OwnerPhone { get; init; } = string.Empty;
    public Guid? PackageId { get; init; }
    public List<PackageFeatureRequestDto>? CustomFeatures { get; init; }
    public decimal? CustomMonthlyPrice { get; init; }
    public decimal? CustomYearlyPrice { get; init; }
    public BillingCycle BillingCycle { get; init; } = BillingCycle.Monthly;
    public DateTime StartDate { get; init; }
    public int TrialDays { get; init; } = 0;
    public bool AutoRenew { get; init; } = true;
    public PaymentType PaymentType { get; init; }
}
