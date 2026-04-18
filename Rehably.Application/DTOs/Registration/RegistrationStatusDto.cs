using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Registration;

public record RegistrationStatusDto(
    Guid OnboardingId,
    OnboardingStep CurrentStep,
    ClinicStatus ClinicStatus,
    OnboardingType? OnboardingType
);
