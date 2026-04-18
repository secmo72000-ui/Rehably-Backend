namespace Rehably.Application.DTOs.Platform;

public record LoginHistoryDto
{
    public DateTime Date { get; init; }
    public string Time { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
    public string Action { get; init; } = "Login";
}
