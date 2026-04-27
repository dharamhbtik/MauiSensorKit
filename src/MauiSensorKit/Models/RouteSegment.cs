namespace MauiSensorKit;

/// <summary>
/// Represents a segment of a route with consistent activity.
/// </summary>
public class RouteSegment
{
    /// <summary>
    /// Start point of the segment.
    /// </summary>
    public RoutePoint Start { get; set; } = new();
    
    /// <summary>
    /// End point of the segment.
    /// </summary>
    public RoutePoint End { get; set; } = new();
    
    /// <summary>
    /// Activity during this segment.
    /// </summary>
    public string Activity { get; set; } = "Unknown";
    
    /// <summary>
    /// Color for this segment based on activity.
    /// </summary>
    public string SegmentColor => GetActivityColor(Activity);
    
    /// <summary>
    /// Distance of this segment in meters.
    /// </summary>
    public double DistanceMeters { get; set; }
    
    /// <summary>
    /// Duration of this segment.
    /// </summary>
    public TimeSpan Duration => End.Timestamp - Start.Timestamp;
    
    /// <summary>
    /// Gets the color for an activity.
    /// </summary>
    public static string GetActivityColor(string activity) => activity switch
    {
        "Walking" or "Walk" => "#00C896",      // Green
        "Running" or "Run" => "#FF6B6B",       // Red
        "In Car/Bus" or "Driving" => "#6C63FF", // Purple
        "On Train/Metro" or "OnTrain" => "#00E5FF", // Cyan
        "On Bus" => "#FF8C42",                // Orange
        "On Stairs" or "Stairs" => "#FFB347", // Amber
        "In Lift/Escalator" or "Elevator" => "#C084FC", // Violet
        "Stationary" or "Standing" or "Sitting" => "#6B6B8A", // Gray
        "Random Motion" => "#3D3D5C",         // Dark muted
        _ => "#6C63FF"                         // Default purple
    };
    
    /// <summary>
    /// Gets the emoji for an activity.
    /// </summary>
    public static string GetActivityEmoji(string activity) => activity switch
    {
        "Walking" or "Walk" => "🚶",
        "Running" or "Run" => "🏃",
        "In Car/Bus" or "Driving" => "🚗",
        "On Train/Metro" or "OnTrain" => "🚆",
        "On Bus" => "🚌",
        "On Stairs" or "Stairs" => "🪜",
        "In Lift/Escalator" or "Elevator" => "🛗",
        "Stationary" => "🛑",
        "Standing" => "🧍",
        "Sitting" => "🪑",
        "Random Motion" => "🔄",
        _ => "❓"
    };
}
