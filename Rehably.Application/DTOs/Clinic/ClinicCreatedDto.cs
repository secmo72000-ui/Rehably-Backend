using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.Clinic;

/// <summary>Response returned after a new clinic has been fully activated.</summary>
public record ClinicCreatedDto
{
    /// <summary>Unique identifier of the created clinic.</summary>
    public Guid Id { get; init; }

    /// <summary>Display name of the clinic.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Arabic display name of the clinic.</summary>
    public string? NameArabic { get; init; }

    /// <summary>URL slug for the clinic.</summary>
    public string Slug { get; init; } = string.Empty;

    /// <summary>Owner's email address.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Clinic phone number.</summary>
    public string Phone { get; init; } = string.Empty;

    /// <summary>Temporary password generated for the clinic owner's first login.</summary>
    public string? TempPassword { get; init; }

    /// <summary>Current activation status of the clinic.</summary>
    public ClinicStatus Status { get; init; }

    /// <summary>When the clinic was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>ID of the subscription created for this clinic.</summary>
    public Guid SubscriptionId { get; init; }

    /// <summary>Name of the package assigned to this clinic.</summary>
    public string? PackageName { get; init; }

    /// <summary>Current subscription status.</summary>
    public SubscriptionStatus SubscriptionStatus { get; init; }

    /// <summary>Subscription start date.</summary>
    public DateTime SubscriptionStartDate { get; init; }

    /// <summary>Subscription end date.</summary>
    public DateTime? SubscriptionEndDate { get; init; }

    /// <summary>How the subscription was paid for (Cash, Online, Free).</summary>
    public string PaymentType { get; init; } = string.Empty;

    /// <summary>Transaction ID returned by the payment provider.</summary>
    public string? PaymentTransactionId { get; init; }

    /// <summary>External payment reference supplied by the admin.</summary>
    public string? PaymentReference { get; init; }

    /// <summary>Stripe Checkout URL to redirect the clinic owner to complete payment. Only set when PaymentType is Online.</summary>
    public string? PaymentUrl { get; init; }
}
