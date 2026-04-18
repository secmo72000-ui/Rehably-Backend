using FluentAssertions;
using FluentValidation.TestHelper;
using Rehably.Application.Validators;

namespace Rehably.Tests.Unit.Validators;

public class SlugValidatorTests
{
    private readonly SlugValidator _validator = new();

    [Theory]
    [InlineData("my-clinic")]
    [InlineData("abc")]
    [InlineData("clinic-name-123")]
    [InlineData("a1b2c3")]
    public void Validate_ValidSlug_PassesValidation(string slug)
    {
        var result = _validator.TestValidate(slug);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_TooShort_HasError()
    {
        var result = _validator.TestValidate("ab");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_TooLong_HasError()
    {
        var slug = new string('a', 51);
        var result = _validator.TestValidate(slug);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_StartsWithHyphen_HasError()
    {
        var result = _validator.TestValidate("-clinic");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_EndsWithHyphen_HasError()
    {
        var result = _validator.TestValidate("clinic-");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_UpperCase_HasError()
    {
        var result = _validator.TestValidate("MyClinic");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_SpecialChars_HasError()
    {
        var result = _validator.TestValidate("my_clinic!");
        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("api")]
    [InlineData("www")]
    [InlineData("app")]
    [InlineData("mail")]
    [InlineData("ftp")]
    [InlineData("test")]
    [InlineData("staging")]
    [InlineData("dev")]
    public void Validate_ReservedWord_HasError(string slug)
    {
        var result = _validator.TestValidate(slug);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ConsecutiveHyphens_HasError()
    {
        var result = _validator.TestValidate("my--clinic");
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ExactlyThreeChars_PassesValidation()
    {
        var result = _validator.TestValidate("abc");
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ExactlyFiftyChars_PassesValidation()
    {
        var slug = new string('a', 50);
        var result = _validator.TestValidate(slug);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_AllDigits_PassesValidation()
    {
        var result = _validator.TestValidate("123456");
        result.ShouldNotHaveAnyValidationErrors();
    }
}
