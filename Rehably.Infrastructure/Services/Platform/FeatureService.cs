using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;

namespace Rehably.Infrastructure.Services.Platform;

/// <summary>
/// Service for managing features.
/// </summary>
public class FeatureService : IFeatureService
{
    private readonly IFeatureRepository _featureRepository;
    private readonly IFeatureCategoryRepository _categoryRepository;
    private readonly IFeaturePricingService _pricingService;
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string FeaturesCacheKey = "Features:All";
    private static readonly TimeSpan FeaturesCacheDuration = TimeSpan.FromHours(1);

    public FeatureService(
        IFeatureRepository featureRepository,
        IFeatureCategoryRepository categoryRepository,
        IFeaturePricingService pricingService,
        IMemoryCache cache,
        IUnitOfWork unitOfWork)
    {
        _featureRepository = featureRepository;
        _categoryRepository = categoryRepository;
        _pricingService = pricingService;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FeatureDto>> GetFeatureByIdAsync(Guid id)
    {
        var feature = await _featureRepository.GetByIdAsync(id);

        if (feature == null || feature.IsDeleted)
            return Result<FeatureDto>.Failure("Feature not found");

        return Result<FeatureDto>.Success(feature.Adapt<FeatureDto>());
    }

    public async Task<Result<List<FeatureDto>>> GetFeaturesAsync(Guid? categoryId = null)
    {
        var cacheKey = categoryId.HasValue
            ? $"Features:Category:{categoryId.Value}"
            : FeaturesCacheKey;

        if (_cache.TryGetValue(cacheKey, out List<FeatureDto>? cachedFeatures))
            return Result<List<FeatureDto>>.Success(cachedFeatures!);

        List<Feature> features;
        if (categoryId.HasValue)
        {
            features = (await _featureRepository.GetByCategoryAsync(categoryId.Value)).ToList();
        }
        else
        {
            features = (await _featureRepository.GetActiveFeaturesAsync()).ToList();
        }

        var featureDtos = features.Adapt<List<FeatureDto>>();
        _cache.Set(cacheKey, featureDtos, FeaturesCacheDuration);

        return Result<List<FeatureDto>>.Success(featureDtos);
    }

    public async Task<Result<FeatureDetailDto>> GetFeatureWithDetailsAsync(Guid id)
    {
        var featureEntity = await _featureRepository.GetByIdAsync(id);

        if (featureEntity == null || featureEntity.IsDeleted)
            return Result<FeatureDetailDto>.Failure("Feature not found");

        var category = await _categoryRepository.GetByIdAsync(featureEntity.CategoryId);

        var feature = featureEntity.Adapt<FeatureDetailDto>();
        if (category != null)
        {
            feature = feature with { Category = category.Adapt<FeatureCategoryDto>() };
        }

        return Result<FeatureDetailDto>.Success(feature);
    }

    public async Task<Result<FeatureDto>> CreateFeatureAsync(CreateFeatureRequestDto request)
    {
        var categoryExists = await _categoryRepository.ExistsAsync(request.CategoryId);
        if (!categoryExists)
            return Result<FeatureDto>.Failure("Feature category not found");

        var existingFeature = await _featureRepository.GetByCodeAsync(request.Code);
        if (existingFeature != null && !existingFeature.IsDeleted)
            return Result<FeatureDto>.Failure("A feature with this code already exists");

        var feature = new Feature
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            PricingType = request.PricingType,
            IsActive = true,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = DateTime.UtcNow
        };

        await _featureRepository.AddAsync(feature);
        await _unitOfWork.SaveChangesAsync();

        await InvalidateFeaturesCacheAsync();

        return Result<FeatureDto>.Success(feature.Adapt<FeatureDto>());
    }

    public async Task<Result<FeatureDto>> UpdateFeatureAsync(Guid id, UpdateFeatureRequestDto request)
    {
        var feature = await _featureRepository.GetByIdAsync(id);

        if (feature == null || feature.IsDeleted)
            return Result<FeatureDto>.Failure("Feature not found");

        feature.Name = request.Name;
        feature.Description = request.Description;
        feature.DisplayOrder = request.DisplayOrder;
        feature.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        await InvalidateFeaturesCacheAsync();

        return Result<FeatureDto>.Success(feature.Adapt<FeatureDto>());
    }

    public async Task<Result> DeactivateFeatureAsync(Guid id)
    {
        var safetyCheck = await _pricingService.CanDeactivateFeatureAsync(id);
        if (!safetyCheck.IsSuccess)
            return Result.Failure(safetyCheck.Error);

        var feature = await _featureRepository.GetByIdAsync(id);

        if (feature == null || feature.IsDeleted)
            return Result.Failure("Feature not found");

        feature.IsActive = false;
        feature.IsDeleted = true;
        feature.DeletedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        await InvalidateFeaturesCacheAsync();

        return Result.Success();
    }

    private async Task InvalidateFeaturesCacheAsync()
    {
        _cache.Remove(FeaturesCacheKey);
        var categoryIds = await _categoryRepository.GetActiveCategoryIdsAsync();

        foreach (var categoryId in categoryIds)
        {
            _cache.Remove($"Features:Category:{categoryId}");
        }
    }

}
