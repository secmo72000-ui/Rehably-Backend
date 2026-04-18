namespace Rehably.Infrastructure.Settings;

public class PaymentSettings
{
    public string DefaultProvider { get; set; } = string.Empty;
    public List<PaymentProviderConfig> Providers { get; set; } = new();
}
