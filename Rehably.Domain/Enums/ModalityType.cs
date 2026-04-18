namespace Rehably.Domain.Enums;

/// <summary>
/// Categories of therapeutic modalities used in physiotherapy.
/// </summary>
public enum ModalityType
{
    /// <summary>Electrical stimulation therapies (TENS, EMS, etc.)</summary>
    Electrotherapy = 1,

    /// <summary>Hands-on therapeutic techniques</summary>
    Manual = 2,

    /// <summary>Heat and cold therapies</summary>
    Thermal = 3,

    /// <summary>Mechanical devices (traction, compression, etc.)</summary>
    Mechanical = 4,

    /// <summary>Water-based therapies</summary>
    Hydrotherapy = 5
}
