namespace MauiSensorKit;

/// <summary>
/// Represents the power source for the device.
/// </summary>
public enum BatteryPowerSource
{
    /// <summary>
    /// Device is running on battery power.
    /// </summary>
    Battery,

    /// <summary>
    /// Device is connected to AC power.
    /// </summary>
    Ac,

    /// <summary>
    /// Device is connected to USB power.
    /// </summary>
    Usb,

    /// <summary>
    /// Device is connected to a wireless charger.
    /// </summary>
    Wireless,

    /// <summary>
    /// Power source is unknown.
    /// </summary>
    Unknown
}
