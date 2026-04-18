using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Billing;

// ── Queries ──────────────────────────────────────────────────────────
public record InsuranceProviderDto(
    Guid Id,
    string Name,
    string? NameArabic,
    string? Country,
    string? LogoUrl,
    bool IsGlobal,
    bool IsActive
);

public record ClinicInsuranceProviderDto(
    Guid Id,
    Guid InsuranceProviderId,
    string ProviderName,
    string? ProviderNameArabic,
    string? Country,
    string? LogoUrl,
    bool PreAuthRequired,
    decimal DefaultCoveragePercent,
    bool IsActive,
    string? Notes,
    List<InsuranceServiceRuleDto> ServiceRules
);

public record InsuranceServiceRuleDto(
    Guid Id,
    BillingServiceType ServiceType,
    CoverageType CoverageType,
    decimal CoverageValue,
    string? Notes
);

public record PatientInsuranceDto(
    Guid Id,
    Guid PatientId,
    Guid ClinicInsuranceProviderId,
    string ProviderName,
    string? ProviderNameArabic,
    string? PolicyNumber,
    string? MembershipId,
    string? HolderName,
    decimal CoveragePercent,
    decimal? MaxAnnualCoverageAmount,
    DateTime? ExpiryDate,
    bool IsActive
);

public record InsuranceClaimDto(
    Guid Id,
    Guid PatientId,
    string PatientName,
    Guid PatientInsuranceId,
    string ProviderName,
    Guid? InvoiceId,
    string? InvoiceNumber,
    string? ClaimNumber,
    ClaimStatus Status,
    DateTime? SubmittedAt,
    decimal? ApprovedAmount,
    decimal? PaidAmount,
    string? RejectedReason,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

// ── Commands ─────────────────────────────────────────────────────────
public record ActivateInsuranceProviderRequest(
    Guid InsuranceProviderId,
    bool PreAuthRequired,
    decimal DefaultCoveragePercent,
    string? Notes,
    List<UpsertServiceRuleRequest>? ServiceRules
);

public record UpdateClinicInsuranceProviderRequest(
    bool PreAuthRequired,
    decimal DefaultCoveragePercent,
    string? Notes,
    List<UpsertServiceRuleRequest>? ServiceRules
);

public record UpsertServiceRuleRequest(
    BillingServiceType ServiceType,
    CoverageType CoverageType,
    decimal CoverageValue,
    string? Notes
);

public record AddPatientInsuranceRequest(
    Guid PatientId,
    Guid ClinicInsuranceProviderId,
    string? PolicyNumber,
    string? MembershipId,
    string? HolderName,
    decimal CoveragePercent,
    decimal? MaxAnnualCoverageAmount,
    DateTime? ExpiryDate
);

public record UpdatePatientInsuranceRequest(
    string? PolicyNumber,
    string? MembershipId,
    string? HolderName,
    decimal CoveragePercent,
    decimal? MaxAnnualCoverageAmount,
    DateTime? ExpiryDate,
    bool IsActive
);

public record SubmitClaimRequest(
    Guid PatientInsuranceId,
    Guid? InvoiceId,
    string? Notes
);

public record UpdateClaimRequest(
    ClaimStatus Status,
    string? ClaimNumber,
    decimal? ApprovedAmount,
    decimal? PaidAmount,
    string? RejectedReason,
    string? Notes
);

public record InsuranceQueryParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public string? Country { get; init; }
}

public record ClaimQueryParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public ClaimStatus? Status { get; init; }
    public Guid? PatientId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}
