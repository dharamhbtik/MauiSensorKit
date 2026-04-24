namespace MauiSensorKit;

/// <summary>
/// Represents a step counter sensor reading with cumulative step count since device reboot.
/// </summary>
public sealed record StepCounterReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.StepCounter;

    /// <summary>
    /// Gets the cumulative step count since the last device reboot.
    /// </summary>
    public long TotalSteps { get; init; }

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Step Counter: {TotalSteps:N0} steps";
    }
}
