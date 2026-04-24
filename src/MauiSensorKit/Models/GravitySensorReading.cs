namespace MauiSensorKit;

/// <summary>
/// Represents a gravity sensor reading measuring the direction and magnitude of gravity in m/s².
/// </summary>
public sealed record GravitySensorReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.GravitySensor;

    /// <summary>
    /// Gets the X-axis component of the gravity vector in m/s².
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Gets the Y-axis component of the gravity vector in m/s².
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Gets the Z-axis component of the gravity vector in m/s².
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// Gets the magnitude of gravity (should be approximately 9.81 m/s² on Earth).
    /// </summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Gravity: X={X:F3}, Y={Y:F3}, Z={Z:F3} m/s² (Mag={Magnitude:F2})";
    }
}
