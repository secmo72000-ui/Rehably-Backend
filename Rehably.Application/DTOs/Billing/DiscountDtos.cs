using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Billing;

public record DiscountDto(
    Guid Id,
    string Name,
    string? NameArabic,
    string? Code,
    DiscountType Type,
    decimal Value,
    DiscountAppliesTo AppliesTo,
    DiscountApplicationMethod ApplicationMethod,
    string? AutoCondition,
    bool IsActive,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    int? MaxUsageTotal,
    int? MaxUsagePerPatient,
    int UsageCount,
    decimal TotalValueGiven,
    SessionPackageOfferDto? PackageOffer
);

public record SessionPackageOfferDto(
    Guid Id,
    int SessionsToPurchase,
    int SessionsFree,
    string? ValidForServiceType
);

public record DiscountUsageDto(
    Guid Id,
    Guid DiscountId,
    string DiscountName,
    Guid PatientId,
    string PatientName,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal AmountApplied,
    DateTime AppliedAt,
    string AppliedByUserId
);

public record CreateDiscountRequest(
    string Name,
    string? NameArabic,
    string? Code,
    DiscountType Type,
    decimal Value,
    DiscountAppliesTo AppliesTo,
    DiscountApplicationMethod ApplicationMethod,
    string? AutoCondition,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    int? MaxUsageTotal,
    int? MaxUsagePerPatient,
    CreatePackageOfferRequest? PackageOffer
);

public record CreatePackageOfferRequest(
    int SessionsToPurchase,
    int SessionsFree,
    string? ValidForServiceType
);

public record UpdateDiscountRequest(
    string Name,
    string? NameArabic,
    string? Code,
    DiscountType Type,
    decimal Value,
    DiscountAppliesTo AppliesTo,
    DiscountApplicationMethod ApplicationMethod,
    string? AutoCondition,
    bool IsActive,
    DateTime? StartsAt,
    DateTime? ExpiresAt,
    int? MaxUsageTotal,
    int? MaxUsagePerPatient
);

public record ValidateDiscountRequest(
    string Code,
    Guid? PatientId,
    DiscountAppliesTo AppliesTo,
    decimal SubTotal
);

public record ValidateDiscountResponse(
    bool IsValid,
    string? Message,
    Guid? DiscountId,
    decimal DiscountAmount
);

public record DiscountQueryParams
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public DiscountType? Type { get; init; }
    public bool? IsActive { get; init; }
    public DiscountApplicationMethod? Method { get; init; }
}
