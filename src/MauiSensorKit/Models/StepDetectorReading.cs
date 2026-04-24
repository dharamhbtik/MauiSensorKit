namespace MauiSensorKit;

/// <summary>
/// Represents a step detector event indicating an individual step was detected.
/// </summary>
public sealed record StepDetectorReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.StepDetector;

    /// <summary>
    /// Gets the timestamp when the step was detected.
    /// </summary>
    public DateTimeOffset StepDetectedAt { get; init; }

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Step Detected at {StepDetectedAt:HH:mm:ss.fff}";
    }
}
