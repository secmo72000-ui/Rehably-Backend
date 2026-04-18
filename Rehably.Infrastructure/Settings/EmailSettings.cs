namespace Rehably.Infrastructure.Settings;

public class EmailSettings
{
    public string DefaultProvider { get; set; } = string.Empty;
    public List<EmailProviderConfig> Providers { get; set; } = new();
}
