using Rehably.Domain.Entities.Library;
using Rehably.Domain.Enums;

namespace Rehably.Application.Repositories;

public interface IClinicLibraryOverrideRepository : IRepository<ClinicLibraryOverride>
{
    Task<IEnumerable<ClinicLibraryOverride>> GetByClinicIdAsync(Guid clinicId);
    Task<ClinicLibraryOverride?> GetByClinicAndItemAsync(Guid clinicId, Guid globalItemId, LibraryType libraryType);
    Task<IEnumerable<Guid>> GetHiddenItemIdsAsync(Guid clinicId, LibraryType libraryType);
    Task<IEnumerable<ClinicLibraryOverride>> GetHiddenItemsAsync(Guid clinicId);
    Task<bool> IsItemHiddenAsync(Guid clinicId, Guid globalItemId, LibraryType libraryType);
    Task<IEnumerable<ClinicLibraryOverride>> GetNonHiddenOverridesAsync(Guid clinicId, LibraryType libraryType);
    Task<IEnumerable<ClinicLibraryOverride>> GetByClinicAndTypeAsync(Guid clinicId, LibraryType? type);
}
