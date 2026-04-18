namespace Rehably.Infrastructure.Settings;

public class SmsSettings
{
    public string DefaultProvider { get; set; } = string.Empty;
    public List<SmsProviderConfig> Providers { get; set; } = new();
}
