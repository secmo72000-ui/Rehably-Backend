namespace Rehably.Infrastructure.Settings;

public class CloudinarySettings
{
    public const string SectionName = "CloudinarySettings";

    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
    public string UploadPreset { get; set; } = string.Empty;
    public string Folder { get; set; } = "clinics";

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB default

    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx"
    };
}
