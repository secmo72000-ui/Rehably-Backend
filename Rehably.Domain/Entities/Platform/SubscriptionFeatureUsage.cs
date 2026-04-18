namespace Rehably.Domain.Entities.Platform;

public class SubscriptionFeatureUsage
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid? ClinicId { get; set; }
    public Guid FeatureId { get; set; }
    public int Limit { get; set; }
    public int Used { get; set; }
    public DateTime LastResetAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Subscription Subscription { get; set; } = null!;
    public Feature Feature { get; set; } = null!;
}
