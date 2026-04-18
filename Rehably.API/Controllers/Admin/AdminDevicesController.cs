using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.DTOs.Library;
using Rehably.Application.Services.Library;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Global device management for platform administrators.
/// </summary>
[ApiController]
[Route("api/admin/devices")]
[RequirePermission("library.manage")]
[Produces("application/json")]
[Tags("Admin - Devices")]
public class AdminDevicesController : ControllerBase
{
    private readonly IDeviceService _deviceService;

    public AdminDevicesController(IDeviceService deviceService)
    {
        _deviceService = deviceService;
    }

    /// <summary>
    /// Get all devices with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(LibraryItemListResponse<DeviceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LibraryItemListResponse<DeviceDto>>> GetDevices(
        [FromQuery] Guid? bodyRegionId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _deviceService.GetDevicesAsync(bodyRegionId, null, page, pageSize);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>
    /// Get a specific device by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> GetDevice(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _deviceService.GetDeviceByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Create a new global device
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(104_857_600)]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DeviceDto>> CreateDevice([FromForm] CreateDeviceRequest request, IFormFile? image, CancellationToken cancellationToken = default)
    {
        var dtoWithStream = request with
        {
            ImageStream = image?.OpenReadStream(),
            ImageFileName = image?.FileName,
            ImageContentType = image?.ContentType
        };

        var result = await _deviceService.CreateDeviceAsync(dtoWithStream, clinicId: null);
        if (!result.IsSuccess) return BadRequest(new { error = result.Error });
        return CreatedAtAction(nameof(GetDevice), new { id = result.Value!.Id }, result.Value);
    }

    /// <summary>
    /// Update an existing device
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequestSizeLimit(104_857_600)]
    [ProducesResponseType(typeof(DeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeviceDto>> UpdateDevice(Guid id, [FromForm] UpdateDeviceRequest request, IFormFile? image, CancellationToken cancellationToken = default)
    {
        var dtoWithStream = request with
        {
            ImageStream = image?.OpenReadStream(),
            ImageFileName = image?.FileName,
            ImageContentType = image?.ContentType
        };

        var result = await _deviceService.UpdateDeviceAsync(id, dtoWithStream, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return Ok(result.Value);
    }

    /// <summary>
    /// Delete a device (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDevice(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _deviceService.DeleteDeviceAsync(id, Guid.Empty);
        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                return NotFound(new { error = result.Error });
            return BadRequest(new { error = result.Error });
        }
        return NoContent();
    }
}
