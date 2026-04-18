namespace Rehably.Application.DTOs.Clinic;

public record RejectClinicRequest
{
    public string Reason { get; init; } = string.Empty;
}
