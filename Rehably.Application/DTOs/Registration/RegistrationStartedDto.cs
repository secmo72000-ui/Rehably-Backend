namespace Rehably.Application.DTOs.Registration;

public record RegistrationStartedDto(
    Guid OnboardingId,
    Guid ClinicId,
    string Email,
    int OtpExpiresIn
);
