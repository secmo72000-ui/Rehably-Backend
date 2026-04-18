using System.ComponentModel.DataAnnotations;

namespace Rehably.Application.DTOs.Clinic;

public record RegisterClinicRequest
{
    [Required]
    [MaxLength(200)]
    public string ClinicName { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? ClinicNameArabic { get; init; }

    [Required]
    [MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    public Guid? SubscriptionPlanId { get; init; }

    public Dictionary<string, object>? Settings { get; init; }
}
