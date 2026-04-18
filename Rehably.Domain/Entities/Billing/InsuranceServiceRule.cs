using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Billing;

public class InsuranceServiceRule
{
    public Guid Id { get; set; }
    public Guid ClinicInsuranceProviderId { get; set; }
    public BillingServiceType ServiceType { get; set; }
    public CoverageType CoverageType { get; set; }
    public decimal CoverageValue { get; set; }
    public string? Notes { get; set; }

    public ClinicInsuranceProvider ClinicInsuranceProvider { get; set; } = null!;
}
