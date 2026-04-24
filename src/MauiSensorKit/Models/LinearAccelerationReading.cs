namespace MauiSensorKit;

/// <summary>
/// Represents a linear acceleration sensor reading measuring acceleration excluding gravity effects.
/// </summary>
public sealed record LinearAccelerationReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.LinearAcceleration;

    /// <summary>
    /// Gets the X-axis linear acceleration in m/s².
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Gets the Y-axis linear acceleration in m/s².
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Gets the Z-axis linear acceleration in m/s².
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// Gets the magnitude of the linear acceleration vector.
    /// </summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Linear Accel: X={X:F3}, Y={Y:F3}, Z={Z:F3} m/s²";
    }
}
