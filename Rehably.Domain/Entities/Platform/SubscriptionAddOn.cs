using Rehably.Domain.Enums;

namespace Rehably.Domain.Entities.Platform;

public class SubscriptionAddOn
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid? ClinicId { get; set; }
    public Guid FeatureId { get; set; }
    public int Quantity { get; set; }

    /// <summary>
    /// Calculated price based on quantity and feature pricing
    /// </summary>
    public decimal CalculatedPrice { get; set; }

    /// <summary>
    /// JSON snapshot of pricing at time of purchase
    /// </summary>
    public string PriceSnapshot { get; set; } = string.Empty;

    public AddOnStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Transaction ID from payment provider
    /// </summary>
    public string? TransactionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Subscription Subscription { get; set; } = null!;
    public Feature Feature { get; set; } = null!;
}
