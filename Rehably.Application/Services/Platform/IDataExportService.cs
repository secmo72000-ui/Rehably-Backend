using Rehably.Application.Common;

namespace Rehably.Application.Services.Platform;

public interface IDataExportService
{
    Task<Result<byte[]>> ExportClinicDataAsync(Guid clinicId);
}
