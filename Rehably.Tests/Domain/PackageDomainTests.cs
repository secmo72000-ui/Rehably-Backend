using FluentAssertions;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;
using Xunit;

namespace Rehably.Tests.Domain;

public class PackageDomainTests
{
    private static Package CreateBasicPackage(ICollection<PackageFeature>? features = null) => new Package
    {
        Name = "Basic Package",
        Code = "BASIC",
        MonthlyPrice = 299.00m,
        YearlyPrice = 2990.00m,
        CalculatedMonthlyPrice = 299.00m,
        CalculatedYearlyPrice = 2990.00m,
        Status = PackageStatus.Active,
        Features = features ?? new List<PackageFeature>()
    };

    #region HasFeature

    [Fact]
    public void HasFeature_WhenFeatureIsIncluded_ShouldReturnTrue()
    {
        var featureId = Guid.NewGuid();
        var package = CreateBasicPackage(new List<PackageFeature>
        {
            new() { FeatureId = featureId, IsIncluded = true }
        });

        package.HasFeature(featureId).Should().BeTrue();
    }

    [Fact]
    public void HasFeature_WhenFeatureIsNotPresent_ShouldReturnFalse()
    {
        var package = CreateBasicPackage(new List<PackageFeature>
        {
            new() { FeatureId = Guid.NewGuid(), IsIncluded = true }
        });

        package.HasFeature(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void HasFeature_WhenFeatureExistsButIsNotIncluded_ShouldReturnFalse()
    {
        var featureId = Guid.NewGuid();
        var package = CreateBasicPackage(new List<PackageFeature>
        {
            new() { FeatureId = featureId, IsIncluded = false }
        });

        package.HasFeature(featureId).Should().BeFalse();
    }

    [Fact]
    public void HasFeature_WhenFeaturesCollectionIsEmpty_ShouldReturnFalse()
    {
        var package = CreateBasicPackage();

        package.HasFeature(Guid.NewGuid()).Should().BeFalse();
    }

    #endregion

    #region GetFeatureLimit

    [Fact]
    public void GetFeatureLimit_WhenFeatureHasLimit_ShouldReturnLimit()
    {
        var featureId = Guid.NewGuid();
        var package = CreateBasicPackage(new List<PackageFeature>
        {
            new() { FeatureId = featureId, IsIncluded = true, Limit = 100 }
        });

        package.GetFeatureLimit(featureId).Should().Be(100);
    }

    [Fact]
    public void GetFeatureLimit_WhenFeatureHasNoLimit_ShouldReturnNull()
    {
        var featureId = Guid.NewGuid();
        var package = CreateBasicPackage(new List<PackageFeature>
        {
            new() { FeatureId = featureId, IsIncluded = true, Limit = null }
        });

        package.GetFeatureLimit(featureId).Should().BeNull();
    }

    [Fact]
    public void GetFeatureLimit_WhenFeatureNotInPackage_ShouldReturnNull()
    {
        var package = CreateBasicPackage(new List<PackageFeature>
        {
            new() { FeatureId = Guid.NewGuid(), IsIncluded = true, Limit = 100 }
        });

        package.GetFeatureLimit(Guid.NewGuid()).Should().BeNull();
    }

    #endregion

    #region CalculatePrice

    [Fact]
    public void CalculatePrice_WhenMonthlyBilling_ShouldReturnCalculatedMonthlyPrice()
    {
        var package = CreateBasicPackage();

        package.CalculatePrice(BillingCycle.Monthly).Should().Be(299.00m);
    }

    [Fact]
    public void CalculatePrice_WhenYearlyBilling_ShouldReturnCalculatedYearlyPrice()
    {
        var package = CreateBasicPackage();

        package.CalculatePrice(BillingCycle.Yearly).Should().Be(2990.00m);
    }

    #endregion

    #region IsValid

    [Fact]
    public void IsValid_WhenAllPricesAndTrialDaysArePositive_ShouldReturnTrue()
    {
        var package = CreateBasicPackage();

        package.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenAllPricesAndTrialDaysAreZero_ShouldReturnTrue()
    {
        var package = new Package
        {
            Name = "Free Package",
            Code = "FREE",
            MonthlyPrice = 0m,
            YearlyPrice = 0m,
            CalculatedMonthlyPrice = 0m,
            CalculatedYearlyPrice = 0m,
            TrialDays = 0,
            Status = PackageStatus.Active,
            Features = new List<PackageFeature>()
        };

        package.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenMonthlyPriceIsNegative_ShouldReturnFalse()
    {
        var package = new Package
        {
            Name = "Bad Package",
            Code = "BAD",
            MonthlyPrice = -1m,
            YearlyPrice = 2990.00m,
            TrialDays = 14,
            Status = PackageStatus.Active,
            Features = new List<PackageFeature>()
        };

        package.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenYearlyPriceIsNegative_ShouldReturnFalse()
    {
        var package = new Package
        {
            Name = "Bad Package",
            Code = "BAD",
            MonthlyPrice = 299.00m,
            YearlyPrice = -1m,
            TrialDays = 14,
            Status = PackageStatus.Active,
            Features = new List<PackageFeature>()
        };

        package.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenTrialDaysIsNegative_ShouldReturnFalse()
    {
        var package = new Package
        {
            Name = "Bad Package",
            Code = "BAD",
            MonthlyPrice = 299.00m,
            YearlyPrice = 2990.00m,
            TrialDays = -1,
            Status = PackageStatus.Active,
            Features = new List<PackageFeature>()
        };

        package.IsValid().Should().BeFalse();
    }

    #endregion
}
