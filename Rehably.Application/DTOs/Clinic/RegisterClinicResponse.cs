namespace Rehably.Application.DTOs.Clinic;

/// <summary>
/// Response for clinic registration including the clinic and authentication token.
/// </summary>
public record RegisterClinicResponse
{
    public ClinicResponse Clinic { get; init; } = null!;
    public string? Token { get; init; }
    public string? TempToken => Token; // Alias for backward compatibility
}
