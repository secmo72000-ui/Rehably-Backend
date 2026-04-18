using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Rehably.API.Controllers.Admin;
using Rehably.Application.Common;
using Rehably.Application.DTOs.Invoice;
using Rehably.Application.Services.Platform;
using Rehably.Domain.Enums;

namespace Rehably.Tests.Controllers.Admin;

public class InvoicesControllerTests
{
    private readonly Mock<IInvoiceService> _invoiceServiceMock;
    private readonly Mock<ILogger<InvoicesController>> _loggerMock;
    private readonly InvoicesController _sut;
    private readonly Guid _adminId = Guid.NewGuid();

    public InvoicesControllerTests()
    {
        _invoiceServiceMock = new Mock<IInvoiceService>();
        _loggerMock = new Mock<ILogger<InvoicesController>>();
        _sut = new InvoicesController(_invoiceServiceMock.Object, _loggerMock.Object);
        SetupAdminUser(_adminId);
    }

    #region GetAllInvoices

    [Fact]
    public async Task GetAllInvoices_DefaultParams_ReturnsOkWithPagedList()
    {
        var response = new InvoiceListResponseDto
        {
            Items = new List<AdminInvoiceDto> { new() { Id = Guid.NewGuid(), InvoiceNumber = "INV-001" } },
            Page = 1,
            PageSize = 20,
            TotalCount = 1,
            TotalPages = 1
        };
        _invoiceServiceMock
            .Setup(x => x.GetAllInvoicesAsync(null, null, null, null, 1, 20))
            .ReturnsAsync(Result<InvoiceListResponseDto>.Success(response));

        var result = await _sut.GetAllInvoices();

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllInvoices_FilterByClinicId_ReturnsFiltered()
    {
        var clinicId = Guid.NewGuid();
        var response = new InvoiceListResponseDto
        {
            Items = new List<AdminInvoiceDto>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 0
        };
        _invoiceServiceMock
            .Setup(x => x.GetAllInvoicesAsync(clinicId, null, null, null, 1, 20))
            .ReturnsAsync(Result<InvoiceListResponseDto>.Success(response));

        var result = await _sut.GetAllInvoices(clinicId: clinicId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllInvoices_FilterByStatus_ReturnsFiltered()
    {
        var response = new InvoiceListResponseDto
        {
            Items = new List<AdminInvoiceDto>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 0
        };
        _invoiceServiceMock
            .Setup(x => x.GetAllInvoicesAsync(null, InvoiceStatus.Paid, null, null, 1, 20))
            .ReturnsAsync(Result<InvoiceListResponseDto>.Success(response));

        var result = await _sut.GetAllInvoices(status: InvoiceStatus.Paid);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllInvoices_FilterByDateRange_ReturnsFiltered()
    {
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 3, 31);
        var response = new InvoiceListResponseDto
        {
            Items = new List<AdminInvoiceDto>(),
            Page = 1,
            PageSize = 20,
            TotalCount = 0,
            TotalPages = 0
        };
        _invoiceServiceMock
            .Setup(x => x.GetAllInvoicesAsync(null, null, startDate, endDate, 1, 20))
            .ReturnsAsync(Result<InvoiceListResponseDto>.Success(response));

        var result = await _sut.GetAllInvoices(startDate: startDate, endDate: endDate);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetAllInvoices_WithPagination_ReturnsCorrectPage()
    {
        var response = new InvoiceListResponseDto
        {
            Items = new List<AdminInvoiceDto>(),
            Page = 2,
            PageSize = 10,
            TotalCount = 25,
            TotalPages = 3
        };
        _invoiceServiceMock
            .Setup(x => x.GetAllInvoicesAsync(null, null, null, null, 2, 10))
            .ReturnsAsync(Result<InvoiceListResponseDto>.Success(response));

        var result = await _sut.GetAllInvoices(page: 2, pageSize: 10);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    #endregion

    #region GetInvoiceDetail

    [Fact]
    public async Task GetInvoiceDetail_WhenFound_ReturnsOk()
    {
        var invoiceId = Guid.NewGuid();
        var invoice = new AdminInvoiceDto
        {
            Id = invoiceId,
            InvoiceNumber = "INV-001",
            ClinicName = "Test Clinic",
            Amount = 500m,
            TotalAmount = 570m
        };
        _invoiceServiceMock
            .Setup(x => x.GetInvoiceDetailAsync(invoiceId))
            .ReturnsAsync(Result<AdminInvoiceDto>.Success(invoice));

        var result = await _sut.GetInvoiceDetail(invoiceId);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetInvoiceDetail_WhenNotFound_Returns404()
    {
        var invoiceId = Guid.NewGuid();
        _invoiceServiceMock
            .Setup(x => x.GetInvoiceDetailAsync(invoiceId))
            .ReturnsAsync(Result<AdminInvoiceDto>.Failure("Invoice not found"));

        var result = await _sut.GetInvoiceDetail(invoiceId);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region MarkInvoicePaid

    [Fact]
    public async Task MarkInvoicePaid_ValidRequest_ReturnsOk()
    {
        var invoiceId = Guid.NewGuid();
        var request = new MarkInvoicePaidRequest { PaymentMethod = "cash", Notes = "Admin marked paid" };
        var updated = new AdminInvoiceDto
        {
            Id = invoiceId,
            InvoiceNumber = "INV-001",
            PaymentStatus = "Paid",
            PaidAt = DateTime.UtcNow
        };
        _invoiceServiceMock
            .Setup(x => x.MarkInvoiceAsPaidByAdminAsync(invoiceId, request))
            .ReturnsAsync(Result<AdminInvoiceDto>.Success(updated));

        var result = await _sut.MarkInvoicePaid(invoiceId, request);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task MarkInvoicePaid_AlreadyPaid_Returns409()
    {
        var invoiceId = Guid.NewGuid();
        var request = new MarkInvoicePaidRequest { PaymentMethod = "cash" };
        _invoiceServiceMock
            .Setup(x => x.MarkInvoiceAsPaidByAdminAsync(invoiceId, request))
            .ReturnsAsync(Result<AdminInvoiceDto>.Failure("Invoice already exists as paid"));

        var result = await _sut.MarkInvoicePaid(invoiceId, request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task MarkInvoicePaid_InvoiceNotFound_Returns404()
    {
        var invoiceId = Guid.NewGuid();
        var request = new MarkInvoicePaidRequest { PaymentMethod = "cash" };
        _invoiceServiceMock
            .Setup(x => x.MarkInvoiceAsPaidByAdminAsync(invoiceId, request))
            .ReturnsAsync(Result<AdminInvoiceDto>.Failure("Invoice not found"));

        var result = await _sut.MarkInvoicePaid(invoiceId, request);

        result.Result.Should().BeOfType<ObjectResult>();
        var objectResult = (ObjectResult)result.Result!;
        objectResult.StatusCode.Should().Be(404);
    }

    #endregion

    #region DeleteInvoice

    [Fact]
    public async Task DeleteInvoice_WhenFound_ReturnsNoContent()
    {
        var invoiceId = Guid.NewGuid();
        _invoiceServiceMock
            .Setup(x => x.DeleteInvoiceAsync(invoiceId, _adminId))
            .ReturnsAsync(Result<bool>.Success(true));

        var result = await _sut.DeleteInvoice(invoiceId);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteInvoice_WhenNotFound_Returns404()
    {
        var invoiceId = Guid.NewGuid();
        _invoiceServiceMock
            .Setup(x => x.DeleteInvoiceAsync(invoiceId, _adminId))
            .ReturnsAsync(Result<bool>.Failure("Invoice not found"));

        var result = await _sut.DeleteInvoice(invoiceId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DeleteInvoice_HasActiveTransactions_Returns409()
    {
        var invoiceId = Guid.NewGuid();
        _invoiceServiceMock
            .Setup(x => x.DeleteInvoiceAsync(invoiceId, _adminId))
            .ReturnsAsync(Result<bool>.Failure("Invoice has active transactions"));

        var result = await _sut.DeleteInvoice(invoiceId);

        result.Should().BeOfType<ConflictObjectResult>();
    }

    #endregion

    #region GetInvoicePdf

    [Fact]
    public async Task GetInvoicePdf_WhenFound_ReturnsFile()
    {
        var invoiceId = Guid.NewGuid();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        _invoiceServiceMock
            .Setup(x => x.GenerateInvoicePdfAsync(invoiceId))
            .ReturnsAsync(Result<byte[]>.Success(pdfBytes));

        var result = await _sut.GetInvoicePdf(invoiceId);

        result.Should().BeOfType<FileContentResult>();
        var fileResult = (FileContentResult)result;
        fileResult.ContentType.Should().Be("application/pdf");
        fileResult.FileContents.Should().BeEquivalentTo(pdfBytes);
    }

    [Fact]
    public async Task GetInvoicePdf_WhenNotFound_Returns404()
    {
        var invoiceId = Guid.NewGuid();
        _invoiceServiceMock
            .Setup(x => x.GenerateInvoicePdfAsync(invoiceId))
            .ReturnsAsync(Result<byte[]>.Failure("Invoice not found"));

        var result = await _sut.GetInvoicePdf(invoiceId);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Helpers

    private void SetupAdminUser(Guid adminId)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, adminId.ToString()),
            new(ClaimTypes.Role, "PlatformAdmin"),
            new("Permission", "*.*")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    #endregion
}
