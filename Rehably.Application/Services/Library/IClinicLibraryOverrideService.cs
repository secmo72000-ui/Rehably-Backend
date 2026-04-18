using Rehably.Application.Common;
using Rehably.Application.DTOs.Library;
using Rehably.Domain.Enums;

namespace Rehably.Application.Services.Library;

/// <summary>
/// Service interface for clinic library override operations.
/// </summary>
public interface IClinicLibraryOverrideService
{
    /// <summary>
    /// Creates an override for a global library item (hide, rename, or modify).
    /// </summary>
    Task<Result<ClinicLibraryOverrideDto>> CreateOverrideAsync(Guid clinicId, CreateClinicLibraryOverrideRequest request);

    /// <summary>
    /// Updates an existing clinic library override.
    /// </summary>
    Task<Result<ClinicLibraryOverrideDto>> UpdateOverrideAsync(Guid clinicId, Guid overrideId, UpdateClinicLibraryOverrideRequest request);

    /// <summary>
    /// Removes an override, restoring the original global library item behavior.
    /// </summary>
    Task<Result> RemoveOverrideAsync(Guid clinicId, Guid overrideId);

    /// <summary>
    /// Gets all overrides for a clinic, optionally filtered by library type.
    /// </summary>
    Task<Result<List<ClinicLibraryOverrideDto>>> GetClinicOverridesAsync(Guid clinicId, LibraryType? type);

    /// <summary>
    /// Gets a specific override by ID.
    /// </summary>
    Task<Result<ClinicLibraryOverrideDto>> GetOverrideByIdAsync(Guid clinicId, Guid overrideId);
}
