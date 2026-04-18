using Microsoft.AspNetCore.Http;

namespace Rehably.Application.DTOs.Clinic;

/// <summary>Request DTO for importing clinic data via file upload.</summary>
public class ClinicImportRequest
{
    /// <summary>The data file to import (.zip or .json, max 100 MB).</summary>
    public IFormFile? File { get; set; }
}
