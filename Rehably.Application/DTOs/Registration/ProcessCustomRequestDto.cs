using Rehably.Application.DTOs.Package;
using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Registration;

public record ProcessCustomRequestDto(
    List<PackageFeatureRequestDto> Features,
    decimal MonthlyPrice,
    decimal YearlyPrice,
    BillingCycle BillingCycle,
    PaymentType PaymentType
);
