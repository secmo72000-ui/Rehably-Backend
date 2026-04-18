using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.Tests.Controllers.Admin;

public class AdminBodyRegionsControllerTests
{
    private readonly Mock<IBodyRegionService> _bodyRegionServiceMock;
    private readonly AdminBodyRegionsController _sut;

    public AdminBodyRegionsControllerTests()
    {
        _bodyRegionServiceMock = new Mock<IBodyRegionService>();
        _sut = new AdminBodyRegionsController(_bodyRegionServiceMock.Object);
    }

    #region GetBodyRegions

    [Fact]
    public async Task GetBodyRegions_WhenServiceSucceeds_ReturnsOkWithCategories()
    {
        var categories = new List<BodyRegionCategoryDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = "UE",
                Name = "Upper Extremity",
                NameArabic = "الطرف العلوي",
                DisplayOrder = 1,
                IsActive = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "LE",
                Name = "Lower Extremity",
                NameArabic = "الطرف السفلي",
                DisplayOrder = 2,
                IsActive = true
            }
        };

        _bodyRegionServiceMock
            .Setup(x => x.GetBodyRegionsAsync())
            .ReturnsAsync(Result<List<BodyRegionCategoryDto>>.Success(categories));

        var result = await _sut.GetBodyRegions();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(categories);
    }

    [Fact]
    public async Task GetBodyRegions_WhenServiceReturnsEmpty_ReturnsOkWithEmptyList()
    {
        var categories = new List<BodyRegionCategoryDto>();

        _bodyRegionServiceMock
            .Setup(x => x.GetBodyRegionsAsync())
            .ReturnsAsync(Result<List<BodyRegionCategoryDto>>.Success(categories));

        var result = await _sut.GetBodyRegions();

        result.Result.Should().BeOfType<OkObjectResult>();
        var okResult = result.Result as OkObjectResult;
        var value = okResult!.Value as List<BodyRegionCategoryDto>;
        value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBodyRegions_WhenServiceFails_ReturnsBadRequest()
    {
        _bodyRegionServiceMock
            .Setup(x => x.GetBodyRegionsAsync())
            .ReturnsAsync(Result<List<BodyRegionCategoryDto>>.Failure("Failed to retrieve body regions"));

        var result = await _sut.GetBodyRegions();

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    #endregion
}
