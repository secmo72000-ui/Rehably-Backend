using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Clinic;

public record CreateClinicRequest
{
    [Required]
    [MaxLength(200)]
    public string ClinicName { get; init; } = string.Empty;

    [MaxLength(200)]
    public string? ClinicNameArabic { get; init; }

    [Required]
    [MaxLength(20)]
    public string Phone { get; init; } = string.Empty;

    [EmailAddress]
    public string? Email { get; init; }

    [MaxLength(500)]
    public string? Address { get; init; }

    [MaxLength(100)]
    public string? City { get; init; }

    [MaxLength(100)]
    public string? Country { get; init; }

    /// <summary>Governorate / province (e.g. القاهرة). Stored alongside City.</summary>
    [MaxLength(100)]
    public string? Governorate { get; init; }

    /// <summary>Desired URL slug (e.g. my-clinic). Auto-generated from ClinicName if omitted.</summary>
    [MaxLength(100)]
    public string? Slug { get; init; }

    [MaxLength(500)]
    public string? LogoUrl { get; init; }

    [Required]
    public Guid PackageId { get; init; }

    public BillingCycle BillingCycle { get; init; } = BillingCycle.Monthly;

    [Required]
    [EmailAddress]
    public string OwnerEmail { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OwnerFirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string OwnerLastName { get; init; } = string.Empty;

    /// <summary>Payment type: Cash (0), Online (1), Free (2)</summary>
    public PaymentType PaymentType { get; init; } = PaymentType.Cash;

    /// <summary>External reference number for online payments collected outside the system</summary>
    [MaxLength(200)]
    public string? PaymentReference { get; init; }

    /// <summary>Owner ID document (image or PDF)</summary>
    public IFormFile? OwnerIdDocument { get; init; }

    /// <summary>Medical license / syndicate card document (image or PDF)</summary>
    public IFormFile? MedicalLicenseDocument { get; init; }

    public int? CustomTrialDays { get; init; }

    /// <summary>Override subscription start date. Defaults to UtcNow if omitted.</summary>
    public DateTime? SubscriptionStartDate { get; init; }

    /// <summary>Override subscription end date. Calculated from BillingCycle if omitted.</summary>
    public DateTime? SubscriptionEndDate { get; init; }

    public Dictionary<string, object>? Settings { get; init; }
}
