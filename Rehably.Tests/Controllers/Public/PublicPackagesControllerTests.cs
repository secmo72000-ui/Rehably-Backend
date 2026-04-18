using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Public;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Package;
using Rehably.Application.Services.Platform;

namespace Rehably.Tests.Controllers.Public;

public class PublicPackagesControllerTests
{
    private readonly Mock<IPackageService> _packageServiceMock;
    private readonly PackagesController _sut;

    public PublicPackagesControllerTests()
    {
        _packageServiceMock = new Mock<IPackageService>();
        _sut = new PackagesController(_packageServiceMock.Object);
    }

    #region GetPublicPackages

    [Fact]
    public async Task GetPublicPackages_ReturnsOk()
    {
        var packages = new List<PublicPackageDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Basic",
                MonthlyPrice = 49.99m,
                YearlyPrice = 499.99m,
                Tier = "Basic",
                TrialDays = 14,
                IsPopular = false,
                HasMonthly = true,
                HasYearly = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Premium",
                MonthlyPrice = 99.99m,
                YearlyPrice = 999.99m,
                Tier = "Premium",
                TrialDays = 14,
                IsPopular = true,
                HasMonthly = true,
                HasYearly = true
            }
        };

        _packageServiceMock
            .Setup(x => x.GetPublicPackagesAsync())
            .ReturnsAsync(Result<List<PublicPackageDto>>.Success(packages));

        var result = await _sut.GetPublicPackages();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetPublicPackages_Empty_ReturnsOk()
    {
        var packages = new List<PublicPackageDto>();

        _packageServiceMock
            .Setup(x => x.GetPublicPackagesAsync())
            .ReturnsAsync(Result<List<PublicPackageDto>>.Success(packages));

        var result = await _sut.GetPublicPackages();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion
}
