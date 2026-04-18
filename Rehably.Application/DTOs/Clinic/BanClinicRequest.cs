namespace Rehably.Application.DTOs.Clinic;

public record BanClinicRequest
{
    public string Reason { get; init; } = string.Empty;
}
