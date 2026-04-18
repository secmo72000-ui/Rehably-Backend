using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Registration;

public record SubmitGlobalPackageRequestDto(
    Guid PackageId,
    BillingCycle BillingCycle
);
