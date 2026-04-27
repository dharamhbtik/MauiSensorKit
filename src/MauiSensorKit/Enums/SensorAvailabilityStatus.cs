namespace MauiSensorKit;

/// <summary>
/// Represents the availability status of a sensor on the current device.
/// </summary>
public enum SensorAvailabilityStatus
{
    /// <summary>
    /// The sensor exists and is ready to use.
    /// </summary>
    Available,

    /// <summary>
    /// The hardware is not present on this device.
    /// </summary>
    Unavailable,

    /// <summary>
    /// Hardware is present but requires runtime permission to access.
    /// </summary>
    PermissionNeeded,

    /// <summary>
    /// This sensor is outside the scope of MAUI Essentials (e.g., Camera, IR, biometric sensors).
    /// </summary>
    NotSupported,

    /// <summary>
    /// Cannot determine availability at runtime.
    /// </summary>
    Unknown
}

/// <summary>
/// Extension methods for SensorAvailabilityStatus.
/// </summary>
public static class SensorAvailabilityStatusExtensions
{
    /// <summary>
    /// Gets a human-readable label for the availability status.
    /// </summary>
    public static string GetLabel(this SensorAvailabilityStatus status)
    {
        return status switch
        {
            SensorAvailabilityStatus.Available => "Available",
            SensorAvailabilityStatus.Unavailable => "Not on this device",
            SensorAvailabilityStatus.PermissionNeeded => "Requires Permission",
            SensorAvailabilityStatus.NotSupported => "External API Required",
            SensorAvailabilityStatus.Unknown => "Unknown",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Gets a color representing the availability status.
    /// </summary>
    public static Color GetColor(this SensorAvailabilityStatus status)
    {
        return status switch
        {
            SensorAvailabilityStatus.Available => Colors.Green,
            SensorAvailabilityStatus.Unavailable => Colors.Red,
            SensorAvailabilityStatus.PermissionNeeded => Colors.Orange,
            SensorAvailabilityStatus.NotSupported => Colors.Gray,
            SensorAvailabilityStatus.Unknown => Colors.Gray,
            _ => Colors.Gray
        };
    }

    /// <summary>
    /// Determines if the sensor can potentially be used (available or needs permission).
    /// </summary>
    public static bool IsPotentiallyUsable(this SensorAvailabilityStatus status)
    {
        return status is SensorAvailabilityStatus.Available or
               SensorAvailabilityStatus.PermissionNeeded;
    }
}
