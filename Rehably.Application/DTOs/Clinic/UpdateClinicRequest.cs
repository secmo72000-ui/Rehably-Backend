using System.ComponentModel.DataAnnotations;

namespace Rehably.Application.DTOs.Clinic;

public record UpdateClinicRequest
{
    [MaxLength(200)]
    public string? Name { get; init; }

    [MaxLength(200)]
    public string? NameArabic { get; init; }

    [MaxLength(500)]
    public string? Description { get; init; }

    [MaxLength(20)]
    public string? Phone { get; init; }

    [EmailAddress]
    public string? Email { get; init; }

    [MaxLength(500)]
    public string? Address { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }

    public Dictionary<string, object>? Settings { get; init; }
}
