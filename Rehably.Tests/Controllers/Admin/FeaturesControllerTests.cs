using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.Services.Platform;

namespace Rehably.Tests.Controllers.Admin;

public class FeaturesControllerTests
{
    private readonly Mock<IFeatureService> _featureServiceMock;
    private readonly FeaturesController _sut;

    public FeaturesControllerTests()
    {
        _featureServiceMock = new Mock<IFeatureService>();
        _sut = new FeaturesController(_featureServiceMock.Object);
    }

    #region GetFeatures

    [Fact]
    public async Task GetFeatures_WhenFeaturesExist_ReturnsOkWithList()
    {
        var features = new List<FeatureDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Patients", Code = "patients", IsActive = true },
            new() { Id = Guid.NewGuid(), Name = "Appointments", Code = "appointments", IsActive = true }
        };
        _featureServiceMock
            .Setup(x => x.GetFeaturesAsync(null))
            .ReturnsAsync(Result<List<FeatureDto>>.Success(features));

        var result = await _sut.GetFeatures();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFeatures_FilterByCategory_ReturnsFilteredList()
    {
        var categoryId = Guid.NewGuid();
        var features = new List<FeatureDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Patients", Code = "patients", CategoryId = categoryId }
        };
        _featureServiceMock
            .Setup(x => x.GetFeaturesAsync(categoryId))
            .ReturnsAsync(Result<List<FeatureDto>>.Success(features));

        var result = await _sut.GetFeatures(categoryId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetFeatures_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _featureServiceMock
            .Setup(x => x.GetFeaturesAsync(null))
            .ReturnsAsync(Result<List<FeatureDto>>.Success(new List<FeatureDto>()));

        var result = await _sut.GetFeatures();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion
}
