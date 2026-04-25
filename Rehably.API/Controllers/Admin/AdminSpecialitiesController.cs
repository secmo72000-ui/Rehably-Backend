using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinical;
using Rehably.Application.Services.Clinical;

namespace Rehably.API.Controllers.Admin;

[ApiController]
[Route("api/admin/specialities")]
[RequirePermission("platform.manage_clinics")]
[Produces("application/json")]
[Tags("Admin - Specialities")]
public class AdminSpecialitiesController : BaseController
{
    private readonly ISpecialityService _specialityService;

    public AdminSpecialitiesController(ISpecialityService specialityService)
    {
        _specialityService = specialityService;
    }

    // ── Global CRUD ───────────────────────────────────────────────────────────

    /// <summary>Get all specialities</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SpecialityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<SpecialityDto>>> GetAll(
        [FromQuery] bool activeOnly = false,
        CancellationToken ct = default)
    {
        var result = await _specialityService.GetAllAsync(activeOnly, ct);
        return FromResult(result);
    }

    /// <summary>Get speciality by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SpecialityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecialityDto>> GetById(Guid id, CancellationToken ct = default)
    {
        var result = await _specialityService.GetByIdAsync(id, ct);
        return FromResult(result);
    }

    /// <summary>Create a new speciality</summary>
    [HttpPost]
    [RequirePermission("platform.super_admin")]
    [ProducesResponseType(typeof(ApiResponse<SpecialityDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SpecialityDto>> Create(
        [FromBody] CreateSpecialityRequest request,
        CancellationToken ct = default)
    {
        var result = await _specialityService.CreateAsync(request, ct);
        return FromResult(result, 201);
    }

    /// <summary>Update a speciality</summary>
    [HttpPut("{id:guid}")]
    [RequirePermission("platform.super_admin")]
    [ProducesResponseType(typeof(ApiResponse<SpecialityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SpecialityDto>> Update(
        Guid id,
        [FromBody] UpdateSpecialityRequest request,
        CancellationToken ct = default)
    {
        var result = await _specialityService.UpdateAsync(id, request, ct);
        return FromResult(result);
    }

    /// <summary>Delete a speciality</summary>
    [HttpDelete("{id:guid}")]
    [RequirePermission("platform.super_admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct = default)
    {
        var result = await _specialityService.DeleteAsync(id, ct);
        return FromResult(result, 204);
    }

    // ── Clinic assignment ─────────────────────────────────────────────────────

    /// <summary>Get all specialities assigned to a clinic</summary>
    [HttpGet("clinic/{clinicId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<List<ClinicSpecialityDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ClinicSpecialityDto>>> GetClinicSpecialities(
        Guid clinicId,
        CancellationToken ct = default)
    {
        var result = await _specialityService.GetClinicSpecialitiesAsync(clinicId, ct);
        return FromResult(result);
    }

    /// <summary>Assign one or more specialities to a clinic</summary>
    [HttpPost("clinic/{clinicId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AssignToClinic(
        Guid clinicId,
        [FromBody] AssignSpecialitiesRequest request,
        CancellationToken ct = default)
    {
        var result = await _specialityService.AssignToClinicAsync(clinicId, request, ct);
        return FromResult(result);
    }

    /// <summary>Remove a speciality from a clinic</summary>
    [HttpDelete("clinic/{clinicId:guid}/{specialityId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> RemoveFromClinic(
        Guid clinicId,
        Guid specialityId,
        CancellationToken ct = default)
    {
        var result = await _specialityService.RemoveFromClinicAsync(clinicId, specialityId, ct);
        return FromResult(result, 204);
    }
}
