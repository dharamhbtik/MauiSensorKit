namespace MauiSensorKit;

/// <summary>
/// Represents a temperature sensor reading in Celsius.
/// </summary>
public sealed record TemperatureReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Temperature;

    /// <summary>
    /// Gets the temperature in degrees Celsius.
    /// </summary>
    public double TemperatureCelsius { get; init; }

    /// <summary>
    /// Gets the temperature in degrees Fahrenheit.
    /// </summary>
    public double TemperatureFahrenheit => TemperatureCelsius * 9.0 / 5.0 + 32.0;

    /// <summary>
    /// Gets the source of the temperature reading.
    /// </summary>
    public TemperatureSource Source { get; init; } = TemperatureSource.Unknown;

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Temperature: {TemperatureCelsius:F1}°C ({TemperatureFahrenheit:F1}°F) - {Source}";
    }
}
