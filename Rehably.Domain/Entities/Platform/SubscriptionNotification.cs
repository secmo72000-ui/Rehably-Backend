namespace Rehably.Domain.Entities.Platform;

public class SubscriptionNotification
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid? ClinicId { get; set; }
    public NotificationType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationChannel Channel { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public string? Recipient { get; set; }
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Subscription Subscription { get; set; } = null!;
}
