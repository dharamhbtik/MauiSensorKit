namespace MauiSensorKit;

/// <summary>
/// Represents a Hall effect sensor reading for detecting magnetic cover/flip cases.
/// </summary>
public sealed record HallSensorReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.HallSensor;

    /// <summary>
    /// Gets a value indicating whether a magnetic cover is detected as closed.
    /// </summary>
    public bool IsCoverClosed { get; init; }

    /// <summary>
    /// Gets a value indicating whether a magnetic cover is detected as open.
    /// </summary>
    public bool IsCoverOpen => !IsCoverClosed;

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Hall Sensor: Cover {(IsCoverClosed ? "Closed" : "Open")}";
    }
}
