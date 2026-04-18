namespace Rehably.Application.DTOs.Package;

/// <summary>
/// Public-facing feature information within a package.
/// </summary>
public record PublicPackageFeatureDto
{
    /// <summary>Feature ID</summary>
    public Guid FeatureId { get; init; }
    /// <summary>Feature name</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Feature code</summary>
    public string Code { get; init; } = string.Empty;
    /// <summary>Feature category</summary>
    public string? Category { get; init; }
    /// <summary>Usage limit (null = unlimited)</summary>
    public int? Limit { get; init; }
    /// <summary>Icon key for UI rendering</summary>
    public string? IconKey { get; init; }
}
