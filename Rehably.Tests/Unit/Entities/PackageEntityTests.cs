using FluentAssertions;
using Rehably.Domain.Entities.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Unit.Entities;

public class PackageEntityTests
{
    private static Package CreatePackage(ICollection<PackageFeature>? features = null) => new Package
    {
        Name = "Standard",
        Code = "STD",
        MonthlyPrice = 199.00m,
        YearlyPrice = 1990.00m,
        Status = PackageStatus.Active,
        Features = features ?? new List<PackageFeature>()
    };

    #region HasFeature

    [Fact]
    public void HasFeature_FeatureExists_ReturnsTrue()
    {
        var featureId = Guid.NewGuid();
        var package = CreatePackage(new List<PackageFeature>
        {
            new() { FeatureId = featureId }
        });

        package.HasFeature(featureId).Should().BeTrue();
    }

    [Fact]
    public void HasFeature_FeatureNotInPackage_ReturnsFalse()
    {
        var package = CreatePackage(new List<PackageFeature>
        {
            new() { FeatureId = Guid.NewGuid() }
        });

        package.HasFeature(Guid.NewGuid()).Should().BeFalse();
    }

    #endregion

    #region GetPrice

    [Fact]
    public void GetPrice_Monthly_ReturnsMonthlyPrice()
    {
        var package = CreatePackage();

        package.GetPrice(BillingCycle.Monthly).Should().Be(199.00m);
    }

    [Fact]
    public void GetPrice_Yearly_ReturnsYearlyPrice()
    {
        var package = CreatePackage();

        package.GetPrice(BillingCycle.Yearly).Should().Be(1990.00m);
    }

    #endregion

    #region GetFeatureLimit

    [Fact]
    public void GetFeatureLimit_FeatureExists_ReturnsLimit()
    {
        var featureId = Guid.NewGuid();
        var package = CreatePackage(new List<PackageFeature>
        {
            new() { FeatureId = featureId, Limit = 50 }
        });

        package.GetFeatureLimit(featureId).Should().Be(50);
    }

    [Fact]
    public void GetFeatureLimit_FeatureNotInPackage_ReturnsNull()
    {
        var package = CreatePackage(new List<PackageFeature>
        {
            new() { FeatureId = Guid.NewGuid(), Limit = 50 }
        });

        package.GetFeatureLimit(Guid.NewGuid()).Should().BeNull();
    }

    #endregion
}
