namespace MauiSensorKit;

/// <summary>
/// Represents a gyroscope sensor reading measuring rotational velocity in rad/s.
/// </summary>
public sealed record GyroscopeReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Gyroscope;

    /// <summary>
    /// Gets the X-axis angular velocity in radians per second.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Gets the Y-axis angular velocity in radians per second.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Gets the Z-axis angular velocity in radians per second.
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Gyroscope: X={X:F3}, Y={Y:F3}, Z={Z:F3} rad/s";
    }
}
