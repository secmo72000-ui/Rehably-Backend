using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Services.Library;

public interface IBodyRegionService
{
    Task<Result<List<BodyRegionCategoryDto>>> GetBodyRegionsAsync();
    Task<Result<BodyRegionDto>> GetBodyRegionByIdAsync(Guid id);
}
