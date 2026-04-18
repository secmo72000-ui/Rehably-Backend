namespace Rehably.Application.DTOs.Audit;

public record AuditLogListResponseDto
{
    public List<AuditLogDto> Items { get; init; } = new();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
