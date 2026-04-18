using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Rehably.API.Authorization;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Validators.Clinic;

namespace Rehably.API.Controllers.Admin;

/// <summary>
/// Handles asynchronous clinic data import from .zip or .json files.
/// </summary>
[ApiController]
[Route("api/admin/clinics")]
[RequirePermission("platform.manage_clinics")]
[Produces("application/json")]
[Tags("Admin - Clinics")]
public class AdminClinicsImportController : BaseController
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ClinicImportRequestValidator _validator;

    public AdminClinicsImportController(
        IBackgroundJobClient backgroundJobClient,
        ClinicImportRequestValidator validator)
    {
        _backgroundJobClient = backgroundJobClient;
        _validator = validator;
    }

    /// <summary>
    /// Enqueue a background job to import clinic data from a .zip or .json file (max 100 MB).
    /// </summary>
    /// <remarks>
    /// Returns HTTP 202 with a job ID that can be used to track import progress.
    /// The import job processes asynchronously via Hangfire.
    /// </remarks>
    /// <param name="id">The clinic ID to import data into.</param>
    /// <param name="request">The import request containing the .zip or .json file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="202">Import job enqueued successfully. Check jobId for progress.</response>
    /// <response code="400">Invalid file format, exceeds size limit, or validation failed.</response>
    [HttpPost("{id:guid}/import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ImportClinicData(
        Guid id,
        [FromForm] ClinicImportRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errors = validation.Errors.Select(e => e.ErrorMessage).ToList();
            return ValidationError("Import file validation failed", errors);
        }

        var tempFilePath = Path.GetTempFileName();
        await using (var stream = System.IO.File.Create(tempFilePath))
        {
            await request.File!.CopyToAsync(stream, cancellationToken);
        }

        var jobId = _backgroundJobClient.Enqueue<IClinicDataImportJob>(
            job => job.ExecuteAsync(id, tempFilePath, CancellationToken.None));

        return StatusCode(202, ApiResponse<object>.SuccessResponse(
            new { jobId, clinicId = id },
            "Import job enqueued successfully"));
    }
}
