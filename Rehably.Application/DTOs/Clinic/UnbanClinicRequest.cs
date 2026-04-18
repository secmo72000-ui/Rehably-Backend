namespace Rehably.Application.DTOs.Clinic;

public record UnbanClinicRequest
{
    public string? Reason { get; init; }
}
