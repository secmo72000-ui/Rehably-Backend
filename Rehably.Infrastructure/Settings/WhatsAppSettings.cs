namespace Rehably.Infrastructure.Settings;

public class WhatsAppSettings
{
    public string DefaultProvider { get; set; } = string.Empty;
    public List<WhatsAppProviderConfig> Providers { get; set; } = new();
}
