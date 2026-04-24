namespace MauiSensorKit;

/// <summary>
/// Represents an Ultra-Wideband (UWB) sensor reading for precise distance measurement.
/// </summary>
public sealed record UwbReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Uwb;

    /// <summary>
    /// Gets the distance to the peer device in meters.
    /// </summary>
    public double DistanceMeters { get; init; }

    /// <summary>
    /// Gets the angle to the peer device in degrees, if available.
    /// </summary>
    public double? AngleDegrees { get; init; }

    /// <summary>
    /// Gets the identifier of the peer UWB device.
    /// </summary>
    public string PeerDeviceId { get; init; } = string.Empty;

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        var angle = AngleDegrees.HasValue ? $", Angle={AngleDegrees.Value:F1}°" : "";
        return $"UWB: {DistanceMeters:F2}m to {PeerDeviceId}{angle}";
    }
}
