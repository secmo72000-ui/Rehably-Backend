namespace Rehably.Application.DTOs.Registration;

public record StartRegistrationRequest(
    string ClinicName,
    string Email,
    string Phone,
    string OwnerFirstName
);
