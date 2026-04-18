namespace Rehably.Application.DTOs.Invoice;

public record InvoiceListResponseDto
{
    /// <summary>Invoices on this page.</summary>
    public List<AdminInvoiceDto> Items { get; init; } = new();

    /// <summary>Current page number.</summary>
    public int Page { get; init; }

    /// <summary>Page size used.</summary>
    public int PageSize { get; init; }

    /// <summary>Total invoice count across all pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages { get; init; }

    /// <summary>Sum of TotalAmount for invoices on this page.</summary>
    public decimal PageTotal { get; init; }

    /// <summary>Sum of TotalAmount for all invoices (unfiltered).</summary>
    public decimal GrandTotal { get; init; }

    /// <summary>Sum of TotalAmount for all paid invoices matching the current filter.</summary>
    public decimal TotalRevenue { get; init; }
}
