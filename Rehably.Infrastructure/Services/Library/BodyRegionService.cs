using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Library;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Services.Library;

public class BodyRegionService : IBodyRegionService
{
    private readonly IBodyRegionCategoryRepository _bodyRegionRepository;
    private readonly ILogger<BodyRegionService> _logger;

    public BodyRegionService(
        IBodyRegionCategoryRepository bodyRegionRepository,
        ILogger<BodyRegionService> logger)
    {
        _bodyRegionRepository = bodyRegionRepository;
        _logger = logger;
    }

    public async Task<Result<List<BodyRegionCategoryDto>>> GetBodyRegionsAsync()
    {
        var categories = await _bodyRegionRepository.GetActiveAsync();

        var result = categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new BodyRegionCategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                Name = c.Name,
                NameArabic = c.NameArabic,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive
            })
            .ToList();

        return Result<List<BodyRegionCategoryDto>>.Success(result);
    }

    public async Task<Result<BodyRegionDto>> GetBodyRegionByIdAsync(Guid id)
    {
        var region = await _bodyRegionRepository.GetWithItemsAsync(id);

        if (region == null)
            return Result<BodyRegionDto>.Failure("Body region not found");

        return Result<BodyRegionDto>.Success(new BodyRegionDto
        {
            Id = region.Id,
            Code = region.Code,
            Name = region.Name,
            NameArabic = region.NameArabic,
            DisplayOrder = region.DisplayOrder,
            IsActive = region.IsActive
        });
    }
}
