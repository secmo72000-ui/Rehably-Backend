namespace Rehably.Application.Services.Storage;

public interface IStorageService
{
    Task<(string? Url, string? Error)> UploadFileAsync(Guid clinicId, string fileName, Stream stream, string folder = "");
    Task<(string? Url, string? Error)> UploadBase64Async(Guid clinicId, string fileName, string base64Data, string folder = "");
    Task<(bool Success, string? Error)> DeleteFileAsync(Guid clinicId, string publicId);
    Task<(bool Success, string? Error)> DeleteByUrlAsync(Guid clinicId, string url);
    Task<long> GetFileSizeAsync(Guid clinicId, string publicId);
    Task<bool> CanUploadAsync(Guid clinicId, long fileSize);
}
