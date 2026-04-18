namespace Rehably.Domain.Enums;

/// <summary>
/// Defines the subscription tier required to access library items.
/// Higher tiers include access to all lower tier content.
/// </summary>
public enum LibraryAccessTier
{
    /// <summary>Available to all clinics regardless of subscription</summary>
    Free = 0,

    /// <summary>Requires Basic package or higher</summary>
    Basic = 1,

    /// <summary>Requires Premium package or higher</summary>
    Premium = 2,

    /// <summary>Requires Enterprise package only</summary>
    Enterprise = 3
}
