namespace Rehably.Application.DTOs.Registration;

public record StartRegistrationRequestDto(
    string ClinicName,
    string? PreferredSlug,
    string Email,
    string Phone,
    string OwnerFirstName,
    string OwnerLastName
);
