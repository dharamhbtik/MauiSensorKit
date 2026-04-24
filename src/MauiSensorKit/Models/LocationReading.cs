namespace MauiSensorKit;

/// <summary>
/// Represents a GPS/GNSS location sensor reading.
/// </summary>
public sealed record LocationReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Location;

    /// <summary>
    /// Gets the latitude in decimal degrees.
    /// </summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Gets the longitude in decimal degrees.
    /// </summary>
    public double Longitude { get; init; }

    /// <summary>
    /// Gets the altitude in meters above sea level, if available.
    /// </summary>
    public double? AltitudeMeters { get; init; }

    /// <summary>
    /// Gets the accuracy of the location fix in meters, if available.
    /// </summary>
    public double? AccuracyMeters { get; init; }

    /// <summary>
    /// Gets the speed in meters per second, if available.
    /// </summary>
    public double? SpeedMps { get; init; }

    /// <summary>
    /// Gets the speed in kilometers per hour, if available.
    /// </summary>
    public double? SpeedKph => SpeedMps * 3.6;

    /// <summary>
    /// Gets the speed in miles per hour, if available.
    /// </summary>
    public double? SpeedMph => SpeedMps * 2.23694;

    /// <summary>
    /// Gets the course/heading in degrees (0-360), if available.
    /// </summary>
    public double? CourseDegrees { get; init; }

    /// <summary>
    /// Gets the source of the location data.
    /// </summary>
    public LocationSource Source { get; init; } = LocationSource.Unknown;

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        var speed = SpeedKph.HasValue ? $", {SpeedKph.Value:F1} km/h" : "";
        var alt = AltitudeMeters.HasValue ? $", {AltitudeMeters.Value:F1}m" : "";
        return $"Location: {Latitude:F6}, {Longitude:F6}{alt}{speed} ({Source})";
    }
}
