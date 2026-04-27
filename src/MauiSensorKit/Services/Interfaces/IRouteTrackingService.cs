namespace MauiSensorKit;

/// <summary>
/// Interface for route tracking service.
/// </summary>
public interface IRouteTrackingService
{
    /// <summary>
    /// Starts tracking a new route session.
    /// </summary>
    Task StartTrackingAsync(string sessionId, CancellationToken ct = default);
    
    /// <summary>
    /// Stops tracking.
    /// </summary>
    Task StopTrackingAsync();
    
    /// <summary>
    /// Gets whether tracking is currently active.
    /// </summary>
    bool IsTracking { get; }
    
    /// <summary>
    /// Gets the current route session.
    /// </summary>
    RouteSession? CurrentSession { get; }
    
    /// <summary>
    /// Gets all points in the current session.
    /// </summary>
    List<RoutePoint> CurrentPoints { get; }
    
    /// <summary>
    /// Event raised when a new point is added.
    /// </summary>
    event EventHandler<RoutePoint>? PointAdded;
    
    /// <summary>
    /// Event raised when a session is completed.
    /// </summary>
    event EventHandler<RouteSession>? SessionCompleted;
    
    /// <summary>
    /// Gets all stored sessions.
    /// </summary>
    Task<List<RouteSession>> GetAllSessionsAsync();
    
    /// <summary>
    /// Gets a specific session.
    /// </summary>
    Task<RouteSession?> GetSessionAsync(string sessionId);
    
    /// <summary>
    /// Deletes a session.
    /// </summary>
    Task DeleteSessionAsync(string sessionId);
    
    /// <summary>
    /// Computes distance between points using Haversine formula.
    /// </summary>
    double ComputeDistanceMeters(RoutePoint p1, RoutePoint p2);
    
    /// <summary>
    /// Gets the current activity at the last point.
    /// </summary>
    string CurrentActivity { get; }
}
