namespace MauiSensorKit;

/// <summary>
/// Represents a complete route tracking session.
/// </summary>
public class RouteSession
{
    /// <summary>
    /// Session identifier.
    /// </summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString("N");
    
    /// <summary>
    /// When the session started.
    /// </summary>
    public DateTimeOffset StartTime { get; set; }
    
    /// <summary>
    /// When the session ended (null if still active).
    /// </summary>
    public DateTimeOffset? EndTime { get; set; }
    
    /// <summary>
    /// All points in the route.
    /// </summary>
    public List<RoutePoint> Points { get; set; } = new();
    
    /// <summary>
    /// Total distance in meters (computed).
    /// </summary>
    public double TotalDistanceMeters { get; set; }
    
    /// <summary>
    /// Total distance in kilometers.
    /// </summary>
    public double TotalDistanceKm => TotalDistanceMeters / 1000.0;
    
    /// <summary>
    /// Maximum speed recorded in m/s.
    /// </summary>
    public double MaxSpeedMps { get; set; }
    
    /// <summary>
    /// Average speed in m/s.
    /// </summary>
    public double AverageSpeedMps { get; set; }
    
    /// <summary>
    /// Total altitude gain in meters (computed from points).
    /// </summary>
    public double? TotalAltitudeGainMeters { get; set; }
    
    /// <summary>
    /// Dominant activity during the session.
    /// </summary>
    public string DominantActivity { get; set; } = "Unknown";
    
    /// <summary>
    /// Duration of the session.
    /// </summary>
    public TimeSpan Duration => EndTime.HasValue 
        ? EndTime.Value - StartTime 
        : DateTimeOffset.Now - StartTime;
    
    /// <summary>
    /// Formatted duration string.
    /// </summary>
    public string DurationString => $"{Duration.Hours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}";
    
    /// <summary>
    /// Number of points in the route.
    /// </summary>
    public int PointCount => Points.Count;
}
