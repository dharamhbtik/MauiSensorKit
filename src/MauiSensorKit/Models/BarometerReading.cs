namespace MauiSensorKit;

/// <summary>
/// Represents a barometer sensor reading measuring atmospheric pressure.
/// </summary>
public sealed record BarometerReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Barometer;

    /// <summary>
    /// Gets the atmospheric pressure in hectopascals (hPa).
    /// </summary>
    public double PressureHPa { get; init; }

    /// <summary>
    /// Gets the equivalent pressure in millibars (same as hPa).
    /// </summary>
    public double PressureMillibar => PressureHPa;

    /// <summary>
    /// Gets the equivalent pressure in inches of mercury.
    /// </summary>
    public double PressureInHg => PressureHPa * 0.02953;

    /// <summary>
    /// Estimates the relative altitude change in meters from a reference pressure of 1013.25 hPa.
    /// </summary>
    public double EstimatedAltitudeChangeMeters => 44330.0 * (1.0 - Math.Pow(PressureHPa / 1013.25, 0.1903));

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Barometer: {PressureHPa:F2} hPa ({PressureInHg:F2} inHg)";
    }
}
