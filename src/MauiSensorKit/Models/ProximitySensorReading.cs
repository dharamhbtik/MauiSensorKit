namespace MauiSensorKit;

/// <summary>
/// Represents a proximity sensor reading detecting nearby objects.
/// </summary>
public sealed record ProximitySensorReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.ProximitySensor;

    /// <summary>
    /// Gets the distance in centimeters. 0 = near, max value = far.
    /// </summary>
    public double DistanceCm { get; init; }

    /// <summary>
    /// Gets a value indicating whether an object is near (convenience property).
    /// </summary>
    public bool IsNear => DistanceCm < 5.0;

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Proximity: {DistanceCm:F1} cm ({(IsNear ? "Near" : "Far")})";
    }
}
