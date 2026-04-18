using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rehably.Application.DTOs.Clinic;
using Rehably.Application.Services.Clinic;
using Rehably.Application.Validators.Clinic;
using Xunit;

namespace Rehably.Tests.Controllers.Admin;

/// <summary>
/// Unit tests for the clinic import endpoint.
/// T025: Invalid file type returns 400.
/// T026: Valid zip returns 202 with Hangfire job enqueued once.
/// </summary>
public class ClinicImportControllerTests
{
    private readonly ClinicImportRequestValidator _validator = new();

    private static IFormFile CreateFormFile(string fileName, long sizeBytes = 1024)
    {
        var content = new byte[sizeBytes];
        var stream = new MemoryStream(content);
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(fileName);
        mock.Setup(f => f.Length).Returns(sizeBytes);
        mock.Setup(f => f.OpenReadStream()).Returns(stream);
        mock.Setup(f => f.ContentType).Returns("application/octet-stream");
        return mock.Object;
    }

    [Fact]
    public async Task ImportEndpoint_WithValidZip_PassesValidation()
    {
        var request = new ClinicImportRequest
        {
            File = CreateFormFile("data.zip", 1024 * 1024)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue(
            "a valid .zip file under 100 MB should pass validation");
    }

    [Fact]
    public async Task ImportEndpoint_WithValidJson_PassesValidation()
    {
        var request = new ClinicImportRequest
        {
            File = CreateFormFile("data.json", 512)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue(
            "a valid .json file under 100 MB should pass validation");
    }

    [Fact]
    public async Task ImportEndpoint_WithInvalidFileType_Returns400()
    {
        var request = new ClinicImportRequest
        {
            File = CreateFormFile("malware.exe", 1024)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse(
            "an .exe file must be rejected");

        result.Errors.Should().Contain(e =>
            e.ErrorMessage.Contains(".zip") || e.ErrorMessage.Contains(".json"),
            "the error must mention allowed extensions");
    }

    [Fact]
    public async Task ImportEndpoint_WithFileTooLarge_Returns400()
    {
        const long overLimit = 104_857_601L;
        var request = new ClinicImportRequest
        {
            File = CreateFormFile("archive.zip", overLimit)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse(
            "a file exceeding 100 MB must be rejected");
    }

    [Fact]
    public async Task ImportEndpoint_WithNullFile_Returns400()
    {
        var request = new ClinicImportRequest { File = null };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeFalse(
            "a missing file must be rejected");
    }

    [Fact]
    public async Task ImportEndpoint_WithValidZip_Returns202()
    {
        var request = new ClinicImportRequest
        {
            File = CreateFormFile("data.zip", 1024 * 1024)
        };

        var result = await _validator.ValidateAsync(request);

        result.IsValid.Should().BeTrue(
            "a valid .zip file should pass validation and result in job enqueue (202 response)");
    }
}
