namespace Rehably.Domain.Constants;

public class PlatformResource
{
    public string Key { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string NameAr { get; set; } = string.Empty;
    public List<PlatformAction> Actions { get; set; } = new();
}
