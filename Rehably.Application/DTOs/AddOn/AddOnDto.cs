using System.Text.Json.Serialization;
using Rehably.Domain.Enums;

namespace Rehably.Application.DTOs.AddOn;

public record AddOnDto
{
    /// <summary>Add-on identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>The feature this add-on provides.</summary>
    public Guid FeatureId { get; init; }

    /// <summary>Display name of the feature.</summary>
    public string FeatureName { get; init; } = string.Empty;

    /// <summary>Optional usage limit (null = unlimited).</summary>
    public int? Limit { get; init; }

    /// <summary>Price charged for this add-on.</summary>
    public decimal Price { get; init; }

    /// <summary>Payment method used.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentType PaymentType { get; init; }

    /// <summary>Current status of the add-on.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AddOnStatus Status { get; init; }

    /// <summary>Start date of the add-on period.</summary>
    public DateTime StartDate { get; init; }

    /// <summary>End date of the add-on period.</summary>
    public DateTime EndDate { get; init; }
}
