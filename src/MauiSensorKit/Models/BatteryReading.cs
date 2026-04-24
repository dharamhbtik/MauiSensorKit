namespace MauiSensorKit;

/// <summary>
/// Represents a battery sensor reading including charge level and state.
/// </summary>
public sealed record BatteryReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Battery;

    /// <summary>
    /// Gets the battery charge level as a value from 0.0 (empty) to 1.0 (full).
    /// </summary>
    public double ChargeLevel { get; init; }

    /// <summary>
    /// Gets the battery charge level as a percentage (0-100).
    /// </summary>
    public int ChargePercentage => (int)(ChargeLevel * 100);

    /// <summary>
    /// Gets the current battery state (charging, discharging, etc.).
    /// </summary>
    public BatteryState State { get; init; }

    /// <summary>
    /// Gets the current power source.
    /// </summary>
    public BatteryPowerSource PowerSource { get; init; }

    /// <summary>
    /// Gets a value indicating whether the device is currently charging.
    /// </summary>
    public bool IsCharging => State == BatteryState.Charging;

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        var charging = IsCharging ? " (Charging)" : "";
        return $"Battery: {ChargePercentage}% - {State} [{PowerSource}]{charging}";
    }
}
