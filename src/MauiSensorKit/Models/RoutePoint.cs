namespace MauiSensorKit;

/// <summary>
/// Represents a single point on a route with location and context data.
/// </summary>
public class RoutePoint
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Timestamp when the point was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Latitude in decimal degrees.
    /// </summary>
    public double Latitude { get; set; }
    
    /// <summary>
    /// Longitude in decimal degrees.
    /// </summary>
    public double Longitude { get; set; }
    
    /// <summary>
    /// Altitude in meters if available.
    /// </summary>
    public double? AltitudeMeters { get; set; }
    
    /// <summary>
    /// Location accuracy in meters.
    /// </summary>
    public double? AccuracyMeters { get; set; }
    
    /// <summary>
    /// Speed in meters per second.
    /// </summary>
    public double? SpeedMps { get; set; }
    
    /// <summary>
    /// Course/heading in degrees (0-360).
    /// </summary>
    public double? CourseDegrees { get; set; }
    
    /// <summary>
    /// Session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Detected activity at this point.
    /// </summary>
    public string ActivityAtPoint { get; set; } = "Unknown";
    
    /// <summary>
    /// Battery level snapshot at this point (0.0-1.0).
    /// </summary>
    public double? BatteryLevelAtPoint { get; set; }
    
    /// <summary>
    /// Converts speed from m/s to km/h.
    /// </summary>
    public double? SpeedKph => SpeedMps * 3.6;
    
    /// <summary>
    /// Converts speed from m/s to mph.
    /// </summary>
    public double? SpeedMph => SpeedMps * 2.237;
}
