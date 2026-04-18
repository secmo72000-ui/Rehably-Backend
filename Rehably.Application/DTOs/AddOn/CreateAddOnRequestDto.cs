using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.AddOn;

public record CreateAddOnRequestDto
{
    /// <summary>The feature to add as an add-on.</summary>
    public Guid FeatureId { get; init; }

    /// <summary>Optional usage limit for this add-on (null = unlimited).</summary>
    public int? Limit { get; init; }

    /// <summary>Price charged for this add-on.</summary>
    public decimal Price { get; init; }

    /// <summary>Start date for the add-on period.</summary>
    public DateTime StartDate { get; init; }

    /// <summary>End date for the add-on period.</summary>
    public DateTime EndDate { get; init; }

    /// <summary>Payment method used.</summary>
    public PaymentType PaymentType { get; init; }
}
