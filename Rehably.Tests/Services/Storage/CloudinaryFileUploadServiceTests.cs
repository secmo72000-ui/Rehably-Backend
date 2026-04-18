using FluentAssertions;
using Moq;
using Rehably.Application.Common;
using Rehably.Application.Services.Storage;
using Rehably.Domain.Entities.Tenant;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Services.Storage;

/// <summary>
/// Safety-net tests for IFileUploadService contract (T070).
/// Uses interface mocking since the Cloudinary implementation requires
/// live cloud credentials and cannot be unit tested without them.
/// </summary>
public class CloudinaryFileUploadServiceTests
{
    private readonly Mock<IFileUploadService> _serviceMock;

    public CloudinaryFileUploadServiceTests()
    {
        _serviceMock = new Mock<IFileUploadService>();
    }

    // ── UploadFileAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task UploadFileAsync_ValidFile_ReturnsFileUrl()
    {
        var stream = new MemoryStream(new byte[1024]);
        var fileName = "logo.png";
        var folder = "clinic-logos";
        var expectedUrl = "https://res.cloudinary.com/test/image/upload/clinic-logos/logo.png";

        _serviceMock
            .Setup(s => s.UploadFileAsync(stream, fileName, folder, null, default))
            .ReturnsAsync(Result<string>.Success(expectedUrl));

        var result = await _serviceMock.Object.UploadFileAsync(stream, fileName, folder);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedUrl);
        result.Value.Should().StartWith("https://");
    }

    [Fact]
    public async Task UploadFileAsync_WithClinicId_TracksUsageForClinic()
    {
        var clinicId = Guid.NewGuid();
        var stream = new MemoryStream(new byte[2048]);
        var expectedUrl = "https://res.cloudinary.com/test/upload/avatar.jpg";

        _serviceMock
            .Setup(s => s.UploadFileAsync(stream, "avatar.jpg", "avatars", clinicId, default))
            .ReturnsAsync(Result<string>.Success(expectedUrl));

        var result = await _serviceMock.Object.UploadFileAsync(stream, "avatar.jpg", "avatars", clinicId);

        result.IsSuccess.Should().BeTrue();
        _serviceMock.Verify(s => s.UploadFileAsync(stream, "avatar.jpg", "avatars", clinicId, default), Times.Once);
    }

    [Fact]
    public async Task UploadFileAsync_FileTooLarge_ReturnsFailure()
    {
        var oversizeStream = new MemoryStream(new byte[11 * 1024 * 1024]); // 11 MB

        _serviceMock
            .Setup(s => s.UploadFileAsync(oversizeStream, "large.pdf", "docs", null, default))
            .ReturnsAsync(Result<string>.Failure("File size exceeds maximum allowed size of 10MB"));

        var result = await _serviceMock.Object.UploadFileAsync(oversizeStream, "large.pdf", "docs");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("File size exceeds");
    }

    [Fact]
    public async Task UploadFileAsync_InvalidFileExtension_ReturnsFailure()
    {
        var stream = new MemoryStream(new byte[1024]);

        _serviceMock
            .Setup(s => s.UploadFileAsync(stream, "malicious.exe", "docs", null, default))
            .ReturnsAsync(Result<string>.Failure("Invalid file type '.exe'. Allowed types: .pdf, .jpg, .jpeg, .png"));

        var result = await _serviceMock.Object.UploadFileAsync(stream, "malicious.exe", "docs");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid file type");
    }

    [Fact]
    public async Task UploadFileAsync_CloudinaryError_ReturnsFailure()
    {
        var stream = new MemoryStream(new byte[1024]);

        _serviceMock
            .Setup(s => s.UploadFileAsync(stream, "photo.jpg", "photos", null, default))
            .ReturnsAsync(Result<string>.Failure("Failed to upload file: Invalid API credentials"));

        var result = await _serviceMock.Object.UploadFileAsync(stream, "photo.jpg", "photos");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Failed to upload file");
    }

    // ── DeleteFileByUrlAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteFileByUrlAsync_ValidUrl_ReturnsSuccess()
    {
        var url = "https://res.cloudinary.com/test/image/upload/clinic-logos/abc123.png";

        _serviceMock
            .Setup(s => s.DeleteFileByUrlAsync(url, default))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _serviceMock.Object.DeleteFileByUrlAsync(url);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteFileByUrlAsync_InvalidUrl_ReturnsFailure()
    {
        var invalidUrl = "not-a-valid-url";

        _serviceMock
            .Setup(s => s.DeleteFileByUrlAsync(invalidUrl, default))
            .ReturnsAsync(Result<bool>.Failure("Invalid URL format — could not extract public ID"));

        var result = await _serviceMock.Object.DeleteFileByUrlAsync(invalidUrl);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid URL format");
    }

    // ── CanUploadAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CanUploadAsync_WithinStorageLimit_ReturnsTrue()
    {
        var clinicId = Guid.NewGuid();
        var fileSize = 2L * 1024 * 1024; // 2 MB

        _serviceMock
            .Setup(s => s.CanUploadAsync(clinicId, fileSize))
            .ReturnsAsync(true);

        var canUpload = await _serviceMock.Object.CanUploadAsync(clinicId, fileSize);

        canUpload.Should().BeTrue();
    }

    [Fact]
    public async Task CanUploadAsync_ExceedsStorageLimit_ReturnsFalse()
    {
        var clinicId = Guid.NewGuid();
        var fileSize = 600L * 1024 * 1024; // 600 MB - exceeds limit

        _serviceMock
            .Setup(s => s.CanUploadAsync(clinicId, fileSize))
            .ReturnsAsync(false);

        var canUpload = await _serviceMock.Object.CanUploadAsync(clinicId, fileSize);

        canUpload.Should().BeFalse();
    }

    // ── UploadDocumentAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UploadDocumentAsync_ValidDocument_ReturnsClinicDocument()
    {
        var clinicId = Guid.NewGuid();
        var stream = new MemoryStream(new byte[500 * 1024]); // 500 KB
        var document = new ClinicDocument
        {
            DocumentType = DocumentType.OwnerId,
            Status = DocumentStatus.Pending,
            StorageUrl = "https://res.cloudinary.com/test/owner-id.pdf"
        };

        _serviceMock
            .Setup(s => s.UploadDocumentAsync(clinicId, DocumentType.OwnerId, "owner-id.pdf", stream, DocumentStatus.Pending, default))
            .ReturnsAsync(Result<ClinicDocument>.Success(document));

        var result = await _serviceMock.Object.UploadDocumentAsync(clinicId, DocumentType.OwnerId, "owner-id.pdf", stream);

        result.IsSuccess.Should().BeTrue();
        result.Value.DocumentType.Should().Be(DocumentType.OwnerId);
        result.Value.Status.Should().Be(DocumentStatus.Pending);
    }
}
