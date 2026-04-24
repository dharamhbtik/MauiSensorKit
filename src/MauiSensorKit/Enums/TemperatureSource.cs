namespace MauiSensorKit;

/// <summary>
/// Specifies the source of a temperature reading.
/// </summary>
public enum TemperatureSource
{
    /// <summary>
    /// Ambient environmental temperature.
    /// </summary>
    Ambient,

    /// <summary>
    /// Device internal temperature.
    /// </summary>
    Device,

    /// <summary>
    /// CPU temperature.
    /// </summary>
    Cpu,

    /// <summary>
    /// Battery temperature.
    /// </summary>
    Battery,

    /// <summary>
    /// Unknown or unspecified source.
    /// </summary>
    Unknown
}
