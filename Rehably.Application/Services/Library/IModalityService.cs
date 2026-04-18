using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Services.Library;

public interface IModalityService
{
    Task<Result<LibraryItemListResponse<ModalityDto>>> GetModalitiesAsync(Guid? bodyRegionId, string? search, int page, int pageSize);
    Task<Result<ModalityDto>> GetModalityByIdAsync(Guid id);
    Task<Result<ModalityDto>> CreateModalityAsync(CreateModalityRequest request, Guid? clinicId);
    Task<Result<ModalityDto>> UpdateModalityAsync(Guid id, UpdateModalityRequest request, Guid clinicId);
    Task<Result> DeleteModalityAsync(Guid id, Guid clinicId);
}
