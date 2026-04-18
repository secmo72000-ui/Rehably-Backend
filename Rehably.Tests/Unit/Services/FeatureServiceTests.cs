using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Entities.Platform;
using Rehably.Infrastructure.Services.Platform;

namespace Rehably.Tests.Unit.Services;

public class FeatureServiceTests : IDisposable
{
    private readonly Mock<IFeatureRepository> _featureRepoMock;
    private readonly Mock<IFeatureCategoryRepository> _categoryRepoMock;
    private readonly Mock<IFeaturePricingService> _pricingServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly IMemoryCache _cache;
    private readonly FeatureService _service;

    public FeatureServiceTests()
    {
        _featureRepoMock = new Mock<IFeatureRepository>();
        _categoryRepoMock = new Mock<IFeatureCategoryRepository>();
        _pricingServiceMock = new Mock<IFeaturePricingService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new FeatureService(
            _featureRepoMock.Object,
            _categoryRepoMock.Object,
            _pricingServiceMock.Object,
            _cache,
            _unitOfWorkMock.Object);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    [Fact]
    public async Task GetFeaturesAsync_NoFilter_ReturnsAllActiveFeatures()
    {
        var features = new List<Feature>
        {
            new() { Id = Guid.NewGuid(), Name = "Patients", Code = "patients", IsActive = true, CategoryId = Guid.NewGuid() },
            new() { Id = Guid.NewGuid(), Name = "SMS", Code = "sms", IsActive = true, CategoryId = Guid.NewGuid() }
        };
        _featureRepoMock.Setup(r => r.GetActiveFeaturesAsync()).ReturnsAsync(features);

        var result = await _service.GetFeaturesAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetFeaturesAsync_FilterByCategory_ReturnsOnlyCategoryFeatures()
    {
        var categoryId = Guid.NewGuid();
        var features = new List<Feature>
        {
            new() { Id = Guid.NewGuid(), Name = "Patients", Code = "patients", IsActive = true, CategoryId = categoryId }
        };
        _featureRepoMock.Setup(r => r.GetByCategoryAsync(categoryId)).ReturnsAsync(features);

        var result = await _service.GetFeaturesAsync(categoryId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].Name.Should().Be("Patients");
    }

    [Fact]
    public async Task GetFeatureByIdAsync_ExistingId_ReturnsFeature()
    {
        var featureId = Guid.NewGuid();
        var feature = new Feature { Id = featureId, Name = "Patients", Code = "patients", IsActive = true, CategoryId = Guid.NewGuid() };
        _featureRepoMock.Setup(r => r.GetByIdAsync(featureId)).ReturnsAsync(feature);

        var result = await _service.GetFeatureByIdAsync(featureId);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Patients");
    }

    [Fact]
    public async Task GetFeatureByIdAsync_NonExistentId_ReturnsNotFound()
    {
        var featureId = Guid.NewGuid();
        _featureRepoMock.Setup(r => r.GetByIdAsync(featureId)).ReturnsAsync((Feature?)null);

        var result = await _service.GetFeatureByIdAsync(featureId);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
