namespace MauiSensorKit;

/// <summary>
/// Represents the current state of the battery.
/// </summary>
public enum BatteryState
{
    /// <summary>
    /// Battery is currently charging.
    /// </summary>
    Charging,

    /// <summary>
    /// Battery is currently discharging.
    /// </summary>
    Discharging,

    /// <summary>
    /// Battery is fully charged.
    /// </summary>
    Full,

    /// <summary>
    /// Battery is not charging.
    /// </summary>
    NotCharging,

    /// <summary>
    /// Battery state is unknown.
    /// </summary>
    Unknown
}
