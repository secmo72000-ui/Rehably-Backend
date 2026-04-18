using Mapster;
using Microsoft.Extensions.Caching.Memory;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Feature;
using Rehably.Application.Services.Platform;
using Rehably.Application.Repositories;
using Rehably.Domain.Entities.Platform;

namespace Rehably.Infrastructure.Services.Platform;

/// <summary>
/// Service for managing feature categories.
/// </summary>
public class FeatureCategoryService : IFeatureCategoryService
{
    private readonly IFeatureCategoryRepository _categoryRepository;
    private readonly IFeatureRepository _featureRepository;
    private readonly IMemoryCache _cache;
    private readonly IUnitOfWork _unitOfWork;

    private static readonly string CategoriesCacheKey = "FeatureCategories:All";

    public FeatureCategoryService(
        IFeatureCategoryRepository categoryRepository,
        IFeatureRepository featureRepository,
        IMemoryCache cache,
        IUnitOfWork unitOfWork)
    {
        _categoryRepository = categoryRepository;
        _featureRepository = featureRepository;
        _cache = cache;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<FeatureCategoryDto>> GetCategoryByIdAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null || category.IsDeleted)
            return Result<FeatureCategoryDto>.Failure("Feature category not found");

        return Result<FeatureCategoryDto>.Success(category.Adapt<FeatureCategoryDto>());
    }

    public async Task<Result<List<FeatureCategoryDto>>> GetCategoriesAsync()
    {
        var categories = await _categoryRepository.GetActiveCategoriesAsync();
        return Result<List<FeatureCategoryDto>>.Success(categories.Adapt<List<FeatureCategoryDto>>());
    }

    public async Task<Result<FeatureCategoryDetailDto>> GetCategoryWithDetailsAsync(Guid id)
    {
        var categoryEntity = await _categoryRepository.GetByIdAsync(id);

        if (categoryEntity == null || categoryEntity.IsDeleted)
            return Result<FeatureCategoryDetailDto>.Failure("Feature category not found");

        var subCategories = await _categoryRepository.GetSubCategoriesAsync(id);
        var features = await _featureRepository.GetByCategoryAsync(id);

        var category = categoryEntity.Adapt<FeatureCategoryDetailDto>();
        category = category with
        {
            SubCategories = subCategories.Adapt<List<FeatureCategoryDto>>(),
            Features = features.Adapt<List<FeatureDto>>()
        };

        return Result<FeatureCategoryDetailDto>.Success(category);
    }

    public async Task<Result<FeatureCategoryDto>> CreateCategoryAsync(CreateFeatureCategoryRequestDto request)
    {
        var existingCategory = await _categoryRepository.GetByCodeAsync(request.Code);

        if (existingCategory != null && !existingCategory.IsDeleted)
            return Result<FeatureCategoryDto>.Failure("A feature category with this code already exists");

        if (request.ParentCategoryId.HasValue)
        {
            var parentExists = await _categoryRepository.ExistsAsync(request.ParentCategoryId.Value);
            if (!parentExists)
                return Result<FeatureCategoryDto>.Failure("Parent category not found");
        }

        var category = new FeatureCategory
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            Icon = request.Icon,
            DisplayOrder = request.DisplayOrder,
            ParentCategoryId = request.ParentCategoryId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _categoryRepository.AddAsync(category);
        await _unitOfWork.SaveChangesAsync();

        InvalidateCategoriesCache();

        return Result<FeatureCategoryDto>.Success(category.Adapt<FeatureCategoryDto>());
    }

    public async Task<Result<FeatureCategoryDto>> UpdateCategoryAsync(Guid id, UpdateFeatureCategoryRequestDto request)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null || category.IsDeleted)
            return Result<FeatureCategoryDto>.Failure("Feature category not found");

        if (request.ParentCategoryId.HasValue)
        {
            if (request.ParentCategoryId.Value == id)
                return Result<FeatureCategoryDto>.Failure("Cannot set category as its own parent");

            var parentExists = await _categoryRepository.ExistsAsync(request.ParentCategoryId.Value);
            if (!parentExists)
                return Result<FeatureCategoryDto>.Failure("Parent category not found");

            if (await IsDescendantAsync(id, request.ParentCategoryId.Value))
                return Result<FeatureCategoryDto>.Failure("Cannot set parent to a descendant category");

            category.ParentCategoryId = request.ParentCategoryId;
        }
        else
        {
            category.ParentCategoryId = null;
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.Icon = request.Icon;
        category.DisplayOrder = request.DisplayOrder;
        category.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        InvalidateCategoriesCache();

        return Result<FeatureCategoryDto>.Success(category.Adapt<FeatureCategoryDto>());
    }

    public async Task<Result> DeactivateCategoryAsync(Guid id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);

        if (category == null || category.IsDeleted)
            return Result.Failure("Feature category not found");

        category.IsActive = false;
        category.IsDeleted = true;
        category.DeletedAt = DateTime.UtcNow;

        await _unitOfWork.SaveChangesAsync();

        InvalidateCategoriesCache();

        return Result.Success();
    }

    private async Task<bool> IsDescendantAsync(Guid categoryId, Guid potentialParentId)
    {
        return await _categoryRepository.IsDescendantAsync(categoryId, potentialParentId);
    }

    private void InvalidateCategoriesCache()
    {
        _cache.Remove(CategoriesCacheKey);
    }

}
