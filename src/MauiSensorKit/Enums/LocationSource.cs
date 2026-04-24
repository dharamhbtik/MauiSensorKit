namespace MauiSensorKit;

/// <summary>
/// Specifies the source of a location/GPS reading.
/// </summary>
public enum LocationSource
{
    /// <summary>
    /// GPS/GNSS satellite positioning.
    /// </summary>
    Gps,

    /// <summary>
    /// Network-based positioning (WiFi/cell towers).
    /// </summary>
    Network,

    /// <summary>
    /// Fused/combined positioning from multiple sources.
    /// </summary>
    Fused,

    /// <summary>
    /// Unknown or unspecified source.
    /// </summary>
    Unknown
}
