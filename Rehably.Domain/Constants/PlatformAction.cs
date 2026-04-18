namespace Rehably.Domain.Constants;

public class PlatformAction
{
    public string Key { get; set; }
    public string NameEn { get; set; }
    public string NameAr { get; set; }

    public PlatformAction(string key, string nameEn, string nameAr)
    {
        Key = key;
        NameEn = nameEn;
        NameAr = nameAr;
    }
}
