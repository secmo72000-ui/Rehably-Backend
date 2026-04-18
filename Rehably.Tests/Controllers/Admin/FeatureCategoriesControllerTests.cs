using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.Services.Platform;

namespace Rehably.Tests.Controllers.Admin;

public class FeatureCategoriesControllerTests
{
    private readonly Mock<IFeatureCategoryService> _categoryServiceMock;
    private readonly FeatureCategoriesController _sut;

    public FeatureCategoriesControllerTests()
    {
        _categoryServiceMock = new Mock<IFeatureCategoryService>();
        _sut = new FeatureCategoriesController(_categoryServiceMock.Object);
    }

    #region GetCategories

    [Fact]
    public async Task GetCategories_WhenCategoriesExist_ReturnsOkWithList()
    {
        var categories = new List<FeatureCategoryDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Core", Code = "core", IsActive = true, DisplayOrder = 1 },
            new() { Id = Guid.NewGuid(), Name = "Add-ons", Code = "addons", IsActive = true, DisplayOrder = 2 }
        };
        _categoryServiceMock
            .Setup(x => x.GetCategoriesAsync())
            .ReturnsAsync(Result<List<FeatureCategoryDto>>.Success(categories));

        var result = await _sut.GetCategories();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCategories_WhenEmpty_ReturnsOkWithEmptyList()
    {
        _categoryServiceMock
            .Setup(x => x.GetCategoriesAsync())
            .ReturnsAsync(Result<List<FeatureCategoryDto>>.Success(new List<FeatureCategoryDto>()));

        var result = await _sut.GetCategories();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCategories_WhenServiceFails_ReturnsError()
    {
        _categoryServiceMock
            .Setup(x => x.GetCategoriesAsync())
            .ReturnsAsync(Result<List<FeatureCategoryDto>>.Failure("Failed to retrieve categories"));

        var result = await _sut.GetCategories();

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(400);
    }

    #endregion

    #region GetCategoryWithDetails

    [Fact]
    public async Task GetCategoryWithDetails_WhenFound_ReturnsOkWithDetails()
    {
        var categoryId = Guid.NewGuid();
        var detail = new FeatureCategoryDetailDto
        {
            Id = categoryId,
            Name = "Core",
            Code = "core",
            IsActive = true,
            SubCategories = new List<FeatureCategoryDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Sub1", Code = "sub1" }
            },
            Features = new List<FeatureDto>
            {
                new() { Id = Guid.NewGuid(), Name = "Patients", Code = "patients" }
            }
        };
        _categoryServiceMock
            .Setup(x => x.GetCategoryWithDetailsAsync(categoryId))
            .ReturnsAsync(Result<FeatureCategoryDetailDto>.Success(detail));

        var result = await _sut.GetCategoryWithDetails(categoryId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetCategoryWithDetails_WhenNotFound_Returns404()
    {
        var categoryId = Guid.NewGuid();
        _categoryServiceMock
            .Setup(x => x.GetCategoryWithDetailsAsync(categoryId))
            .ReturnsAsync(Result<FeatureCategoryDetailDto>.Failure("Category not found"));

        var result = await _sut.GetCategoryWithDetails(categoryId);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion
}
