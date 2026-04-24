namespace MauiSensorKit;

/// <summary>
/// Represents an ambient light sensor reading measuring illuminance in lux.
/// </summary>
public sealed record AmbientLightReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.AmbientLight;

    /// <summary>
    /// Gets the illuminance in lux.
    /// </summary>
    public double Lux { get; init; }

    /// <summary>
    /// Gets a description of the light level based on lux value.
    /// </summary>
    public string LightLevelDescription => Lux switch
    {
        < 10 => "Pitch Black",
        < 50 => "Very Dark",
        < 100 => "Dark Indoor",
        < 500 => "Dim Indoor",
        < 1000 => "Normal Indoor",
        < 5000 => "Bright Indoor",
        < 10000 => "Overcast Day",
        < 25000 => "Full Daylight",
        _ => "Direct Sunlight"
    };

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Ambient Light: {Lux:F1} lux ({LightLevelDescription})";
    }
}
