using FluentAssertions;
using FluentValidation.TestHelper;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Validators.Platform;

namespace Rehably.Tests.Unit.Validators;

public class CreatePackageRequestDtoValidatorTests
{
    private readonly CreatePackageRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic-plan",
            Description = "Entry level package",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            TrialDays = 14,
            DisplayOrder = 1,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = Guid.NewGuid(), Limit = 50 }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds100Chars_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = new string('a', 101),
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_EmptyCode_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_CodeExceeds50Chars_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = new string('a', 51),
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_CodeWithUpperCase_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "Basic-Plan",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_CodeWithSpecialChars_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic_plan!",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Code);
    }

    [Fact]
    public void Validate_NegativeMonthlyPrice_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = -1m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.MonthlyPrice);
    }

    [Fact]
    public void Validate_NegativeYearlyPrice_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = -1m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.YearlyPrice);
    }

    [Fact]
    public void Validate_ZeroPrices_NoErrors()
    {
        // Zero is valid — allows free-tier packages
        var request = new CreatePackageRequestDto
        {
            Name = "Free Plan",
            Code = "free",
            MonthlyPrice = 0m,
            YearlyPrice = 0m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.MonthlyPrice);
        result.ShouldNotHaveValidationErrorFor(x => x.YearlyPrice);
    }

    [Fact]
    public void Validate_EmptyFeatures_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>()
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Features);
    }

    [Fact]
    public void Validate_DuplicateFeatureIds_HasError()
    {
        var featureId = Guid.NewGuid();
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = featureId, Limit = 10 },
                new() { FeatureId = featureId, Limit = 20 }
            }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Features);
    }

    [Fact]
    public void Validate_DescriptionExceeds1000Chars_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            Description = new string('a', 1001),
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NegativeTrialDays_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            TrialDays = -1,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.TrialDays);
    }

    [Fact]
    public void Validate_NegativeDisplayOrder_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            DisplayOrder = -1,
            Features = new List<PackageFeatureRequestDto> { new() { FeatureId = Guid.NewGuid() } }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }

    [Fact]
    public void Validate_FeatureLimitNegative_HasError()
    {
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = Guid.NewGuid(), Limit = -5 }
            }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_FeatureLimitNull_NoError()
    {
        // Null limit means unlimited — valid
        var request = new CreatePackageRequestDto
        {
            Name = "Basic Plan",
            Code = "basic",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = Guid.NewGuid(), Limit = null }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}

public class UpdatePackageRequestDtoValidatorTests
{
    private readonly UpdatePackageRequestDtoValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_NoErrors()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            Description = "An updated description",
            MonthlyPrice = 150m,
            YearlyPrice = 1500m,
            TrialDays = 7,
            DisplayOrder = 2
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyName_HasError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NameExceeds100Chars_HasError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = new string('a', 101),
            MonthlyPrice = 100m,
            YearlyPrice = 1000m
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_NegativeMonthlyPrice_HasError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            MonthlyPrice = -0.01m,
            YearlyPrice = 1000m
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.MonthlyPrice);
    }

    [Fact]
    public void Validate_NegativeYearlyPrice_HasError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            MonthlyPrice = 100m,
            YearlyPrice = -500m
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.YearlyPrice);
    }

    [Fact]
    public void Validate_DescriptionExceeds1000Chars_HasError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            Description = new string('x', 1001),
            MonthlyPrice = 100m,
            YearlyPrice = 1000m
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NullDescription_NoError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            Description = null,
            MonthlyPrice = 100m,
            YearlyPrice = 1000m
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void Validate_NegativeTrialDays_HasError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            TrialDays = -5
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.TrialDays);
    }

    [Fact]
    public void Validate_NegativeDisplayOrder_HasError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            DisplayOrder = -1
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.DisplayOrder);
    }

    [Fact]
    public void Validate_NullFeatures_NoError()
    {
        // Null means "keep existing features" — the validator skips the Features
        // block entirely when Features is null
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = null
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_DuplicateFeatureIds_HasError()
    {
        var featureId = Guid.NewGuid();
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = featureId, Limit = 10 },
                new() { FeatureId = featureId, Limit = 20 }
            }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
        result.ShouldHaveValidationErrorFor(x => x.Features);
    }

    [Fact]
    public void Validate_ValidFeaturesProvided_NoError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = Guid.NewGuid(), Limit = 50 },
                new() { FeatureId = Guid.NewGuid(), Limit = null }
            }
        };

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_FeatureLimitNegative_HasError()
    {
        var request = new UpdatePackageRequestDto
        {
            Name = "Updated Plan",
            MonthlyPrice = 100m,
            YearlyPrice = 1000m,
            Features = new List<PackageFeatureRequestDto>
            {
                new() { FeatureId = Guid.NewGuid(), Limit = -1 }
            }
        };

        var result = _validator.TestValidate(request);

        result.IsValid.Should().BeFalse();
    }
}
