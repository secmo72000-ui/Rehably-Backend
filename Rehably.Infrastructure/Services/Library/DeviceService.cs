using Mapster;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Repositories;
using Rehably.Application.Services.Library;
using Rehably.Application.Services.Storage;
using Rehably.Domain.Entities.Library;

namespace Rehably.Infrastructure.Services.Library;

public class DeviceService : IDeviceService
{
    private readonly IDeviceRepository _deviceRepository;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<DeviceService> _logger;

    public DeviceService(
        IDeviceRepository deviceRepository,
        IFileUploadService fileUploadService,
        ILogger<DeviceService> logger)
    {
        _deviceRepository = deviceRepository;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    public async Task<Result<LibraryItemListResponse<DeviceDto>>> GetDevicesAsync(Guid? bodyRegionId, string? search, int page, int pageSize)
    {
        var query = _deviceRepository.Query().Where(d => !d.IsDeleted);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(d => d.Name.ToLower().Contains(search.ToLower()));

        var totalCount = await query.CountAsync();

        var devices = (await query
            .OrderBy(d => d.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync())
            .Adapt<List<DeviceDto>>();

        var response = LibraryItemListResponse<DeviceDto>.Create(devices, page, pageSize, totalCount);
        return Result<LibraryItemListResponse<DeviceDto>>.Success(response);
    }

    public async Task<Result<DeviceDto>> GetDeviceByIdAsync(Guid id)
    {
        var device = await _deviceRepository.GetWithDetailsAsync(id);
        if (device == null)
            return Result<DeviceDto>.Failure("Device not found");

        return Result<DeviceDto>.Success(device.Adapt<DeviceDto>());
    }

    public async Task<Result<DeviceDto>> CreateDeviceAsync(CreateDeviceRequest request, Guid? clinicId)
    {
        var device = new Device
        {
            ClinicId = clinicId,
            Name = request.Name,
            NameArabic = request.NameArabic,
            Description = request.Description,
            RelatedConditionCodes = request.RelatedConditionCodes,
            Manufacturer = request.Manufacturer,
            Model = request.Model,
            AccessTier = request.AccessTier,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        if (request.ImageStream != null)
        {
            var folder = clinicId.HasValue
                ? $"rehably/clinics/{clinicId}/devices/images"
                : "rehably/library/devices/images";
            var imageResult = await _fileUploadService.UploadFileAsync(request.ImageStream, request.ImageFileName!, folder, clinicId);
            if (imageResult.IsSuccess)
                device.ImageUrl = imageResult.Value;
        }

        await _deviceRepository.AddAsync(device);
        await _deviceRepository.SaveChangesAsync();

        _logger.LogInformation("Created device {DeviceId} ({DeviceName}) for clinic {ClinicId}",
            device.Id, device.Name, clinicId ?? Guid.Empty);

        return Result<DeviceDto>.Success(device.Adapt<DeviceDto>());
    }

    public async Task<Result<DeviceDto>> UpdateDeviceAsync(Guid id, UpdateDeviceRequest request, Guid clinicId)
    {
        var device = await _deviceRepository.GetWithDetailsAsync(id);
        if (device == null)
            return Result<DeviceDto>.Failure("Device not found");

        if (clinicId != Guid.Empty && device.ClinicId != clinicId)
            return Result<DeviceDto>.Failure("You can only update devices owned by your clinic");

        device.Name = request.Name;
        device.NameArabic = request.NameArabic;
        device.Description = request.Description;
        device.RelatedConditionCodes = request.RelatedConditionCodes;
        device.Manufacturer = request.Manufacturer;
        device.Model = request.Model;
        device.AccessTier = request.AccessTier;
        device.UpdatedAt = DateTime.UtcNow;

        if (request.ImageStream != null)
        {
            if (!string.IsNullOrEmpty(device.ImageUrl))
                await _fileUploadService.DeleteFileByUrlAsync(device.ImageUrl);

            var folder = device.ClinicId.HasValue
                ? $"rehably/clinics/{device.ClinicId}/devices/images"
                : "rehably/library/devices/images";
            var imageResult = await _fileUploadService.UploadFileAsync(request.ImageStream, request.ImageFileName!, folder, device.ClinicId);
            if (imageResult.IsSuccess)
                device.ImageUrl = imageResult.Value;
        }

        await _deviceRepository.UpdateAsync(device);
        await _deviceRepository.SaveChangesAsync();

        _logger.LogInformation("Updated device {DeviceId} ({DeviceName})", device.Id, device.Name);

        return Result<DeviceDto>.Success(device.Adapt<DeviceDto>());
    }

    public async Task<Result> DeleteDeviceAsync(Guid id, Guid clinicId)
    {
        var device = await _deviceRepository.GetByIdAsync(id);
        if (device == null)
            return Result.Failure("Device not found");

        if (clinicId != Guid.Empty && device.ClinicId != clinicId)
            return Result.Failure("You can only delete devices owned by your clinic");

        device.IsDeleted = true;
        device.UpdatedAt = DateTime.UtcNow;

        await _deviceRepository.UpdateAsync(device);
        await _deviceRepository.SaveChangesAsync();

        _logger.LogInformation("Soft deleted device {DeviceId} ({DeviceName})", device.Id, device.Name);

        return Result.Success();
    }

}
