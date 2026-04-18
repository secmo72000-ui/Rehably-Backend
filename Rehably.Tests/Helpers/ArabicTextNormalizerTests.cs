using FluentAssertions;
using Rehably.Infrastructure.Helpers;

namespace Rehably.Tests.Helpers;

public class ArabicTextNormalizerTests
{
    [Fact]
    public void Normalize_NullText_ReturnsEmptyString()
    {
        string? text = null;

        var result = ArabicTextNormalizer.Normalize(text);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Normalize_EmptyText_ReturnsEmptyString()
    {
        var text = string.Empty;

        var result = ArabicTextNormalizer.Normalize(text);

        result.Should().Be(string.Empty);
    }

    [Fact]
    public void Normalize_TextWithTashkeel_RemovesDiacritics()
    {
        var text = "مُحَمَّد";
        var result = ArabicTextNormalizer.Normalize(text);
        result.Should().Be("محمد");
    }

    [Fact]
    public void Normalize_HamzaVariations_NormalizesToAlef()
    {
        var textWithHamzaAbove = "أحمد";
        var textWithHamzaBelow = "إسلام";
        var textWithMaddah = "آمن";
        var result1 = ArabicTextNormalizer.Normalize(textWithHamzaAbove);
        var result2 = ArabicTextNormalizer.Normalize(textWithHamzaBelow);
        var result3 = ArabicTextNormalizer.Normalize(textWithMaddah);
        result1.Should().Be("احمد");
        result2.Should().Be("اسلام");
        result3.Should().Be("امن");
    }

    [Fact]
    public void Normalize_TaaMarbuta_NormalizesToHaa()
    {
        var text = "مدرسة";
        var result = ArabicTextNormalizer.Normalize(text);
        result.Should().Be("مدرسه");
    }

    [Fact]
    public void Normalize_YaaVariations_NormalizesToYaa()
    {
        var text = "مستشفى";
        var result = ArabicTextNormalizer.Normalize(text);
        result.Should().Be("مستشفي");
    }

    [Fact]
    public void Normalize_MixedCase_ConvertsToLowerCase()
    {
        var text = "Cairo Clinic";

        var result = ArabicTextNormalizer.Normalize(text);

        result.Should().Be("cairo clinic");
    }

    [Fact]
    public void Normalize_ComplexArabicText_AppliesAllRules()
    {
        var text = "مُسْتَشْفَى الأَهْرَامِ";
        var result = ArabicTextNormalizer.Normalize(text);
        result.Should().Be("مستشفي الاهرام");
    }

    [Fact]
    public void Normalize_EnglishText_OnlyLowerCase()
    {
        var text = "Cairo Medical Center";

        var result = ArabicTextNormalizer.Normalize(text);

        result.Should().Be("cairo medical center");
    }

    [Fact]
    public void Normalize_MixedArabicEnglish_NormalizesCorrectly()
    {
        var text = "مُسْتَشْفَى Cairo";

        var result = ArabicTextNormalizer.Normalize(text);

        result.Should().Be("مستشفي cairo");
    }

    [Fact]
    public void Normalize_TextWithSpaces_PreservesSpaces()
    {
        var text = "مُحَمَّد أَحْمَد";

        var result = ArabicTextNormalizer.Normalize(text);

        result.Should().Be("محمد احمد");
    }
}
