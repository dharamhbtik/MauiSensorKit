namespace MauiSensorKit;

/// <summary>
/// Represents a battery temperature sensor reading for safety monitoring.
/// </summary>
public sealed record BatteryTemperatureReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.BatteryTemperature;

    /// <summary>
    /// Gets the battery temperature in degrees Celsius.
    /// </summary>
    public double TemperatureCelsius { get; init; }

    /// <summary>
    /// Gets the battery temperature in degrees Fahrenheit.
    /// </summary>
    public double TemperatureFahrenheit => TemperatureCelsius * 9.0 / 5.0 + 32.0;

    /// <summary>
    /// Gets a value indicating whether the battery is overheating (greater than 45°C).
    /// </summary>
    public bool IsOverheating => TemperatureCelsius > 45.0;

    /// <summary>
    /// Gets a value indicating whether the battery temperature is critically high (greater than 60°C).
    /// </summary>
    public bool IsCritical => TemperatureCelsius > 60.0;

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        var warning = IsCritical ? " CRITICAL" : IsOverheating ? " HOT" : "";
        return $"Battery Temp: {TemperatureCelsius:F1}°C{warning}";
    }
}
