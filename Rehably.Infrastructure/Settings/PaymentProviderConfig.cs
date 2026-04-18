namespace Rehably.Infrastructure.Settings;

public class PaymentProviderConfig
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public string? MerchantId { get; set; }
    public string? HmacSecret { get; set; }
    public string? WebhookSecret { get; set; }
    public string? IntegrationId { get; set; }
    public string? FrameUrl { get; set; }
    public string? CallbackUrl { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsDefault { get; set; }
}
