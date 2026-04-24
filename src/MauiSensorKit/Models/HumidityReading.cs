namespace MauiSensorKit;

/// <summary>
/// Represents a humidity sensor reading measuring relative humidity percentage.
/// </summary>
public sealed record HumidityReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Humidity;

    /// <summary>
    /// Gets the relative humidity as a percentage (0-100).
    /// </summary>
    public double RelativeHumidityPercent { get; init; }

    /// <summary>
    /// Gets a description of the humidity comfort level.
    /// </summary>
    public string ComfortLevel => RelativeHumidityPercent switch
    {
        < 30 => "Dry",
        < 60 => "Comfortable",
        < 70 => "Sticky",
        _ => "Uncomfortable"
    };

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Humidity: {RelativeHumidityPercent:F1}% ({ComfortLevel})";
    }
}
