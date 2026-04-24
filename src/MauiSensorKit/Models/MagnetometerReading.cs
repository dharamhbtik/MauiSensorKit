namespace MauiSensorKit;

/// <summary>
/// Represents a magnetometer sensor reading measuring magnetic field strength in microteslas (µT).
/// </summary>
public sealed record MagnetometerReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Magnetometer;

    /// <summary>
    /// Gets the X-axis magnetic field strength in µT.
    /// </summary>
    public double X { get; init; }

    /// <summary>
    /// Gets the Y-axis magnetic field strength in µT.
    /// </summary>
    public double Y { get; init; }

    /// <summary>
    /// Gets the Z-axis magnetic field strength in µT.
    /// </summary>
    public double Z { get; init; }

    /// <summary>
    /// Gets the total magnetic field strength (magnitude) in µT.
    /// </summary>
    public double Magnitude => Math.Sqrt(X * X + Y * Y + Z * Z);

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Magnetometer: X={X:F1}, Y={Y:F1}, Z={Z:F1} µT (Total={Magnitude:F1} µT)";
    }
}
