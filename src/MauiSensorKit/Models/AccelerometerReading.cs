namespace MauiSensorKit;

/// <summary>
/// Represents an accelerometer sensor reading measuring acceleration forces in m/s².
/// </summary>
public sealed record AccelerometerReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Accelerometer;

    /// <summary>
    /// Gets the X-axis acceleration in m/s².
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Gets the Y-axis acceleration in m/s².
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Gets the Z-axis acceleration in m/s².
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// Gets the magnitude of the acceleration vector.
    /// </summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Accelerometer: X={X:F3}, Y={Y:F3}, Z={Z:F3} m/s²";
    }
}
