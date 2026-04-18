using System.Text.RegularExpressions;

namespace Rehably.Infrastructure.Helpers;

public static class ArabicTextNormalizer
{
    /// <summary>
    /// Normalizes Arabic text for search by removing diacritics and normalizing character variations.
    /// </summary>
    /// <param name="text">The text to normalize</param>
    /// <returns>Normalized text in lowercase</returns>
    public static string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = Regex.Replace(text, @"[\u064B-\u065F\u0670]", "");

        text = text.Replace("أ", "ا")
                   .Replace("إ", "ا")
                   .Replace("آ", "ا");

        text = text.Replace("ة", "ه");

        text = text.Replace("ى", "ي");

        return text.ToLowerInvariant();
    }
}
