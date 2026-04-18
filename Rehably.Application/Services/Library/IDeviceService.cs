using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;

namespace Rehably.Application.Services.Library;

public interface IDeviceService
{
    Task<Result<LibraryItemListResponse<DeviceDto>>> GetDevicesAsync(Guid? bodyRegionId, string? search, int page, int pageSize);
    Task<Result<DeviceDto>> GetDeviceByIdAsync(Guid id);
    Task<Result<DeviceDto>> CreateDeviceAsync(CreateDeviceRequest request, Guid? clinicId);
    Task<Result<DeviceDto>> UpdateDeviceAsync(Guid id, UpdateDeviceRequest request, Guid clinicId);
    Task<Result> DeleteDeviceAsync(Guid id, Guid clinicId);
}
