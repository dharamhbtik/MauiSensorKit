namespace MauiSensorKit;

/// <summary>
/// Interface for battery history tracking and analytics.
/// </summary>
public interface IBatteryHistoryService
{
    /// <summary>
    /// Records a battery snapshot.
    /// </summary>
    Task RecordSnapshotAsync(BatterySnapshot snapshot);
    
    /// <summary>
    /// Gets all battery snapshots for a session.
    /// </summary>
    Task<List<BatterySnapshot>> GetSessionHistoryAsync(string sessionId);
    
    /// <summary>
    /// Gets battery snapshots within a date range.
    /// </summary>
    Task<List<BatterySnapshot>> GetHistoryRangeAsync(DateTimeOffset from, DateTimeOffset to);
    
    /// <summary>
    /// Computes analytics for a session.
    /// </summary>
    Task<BatteryAnalytics> ComputeAnalyticsAsync(string sessionId);
    
    /// <summary>
    /// Computes analytics for a date range.
    /// </summary>
    Task<BatteryAnalytics> ComputeAnalyticsRangeAsync(DateTimeOffset from, DateTimeOffset to);
    
    /// <summary>
    /// Gets battery events for a session.
    /// </summary>
    Task<List<BatteryEvent>> GetEventsAsync(string sessionId);
    
    /// <summary>
    /// Event raised when a battery event is detected.
    /// </summary>
    event EventHandler<BatteryEvent>? BatteryEventDetected;
    
    /// <summary>
    /// Gets the current session ID being tracked.
    /// </summary>
    string? CurrentSessionId { get; }
    
    /// <summary>
    /// Gets the last recorded snapshot.
    /// </summary>
    BatterySnapshot? LastSnapshot { get; }
    
    /// <summary>
    /// Starts tracking for a session.
    /// </summary>
    Task StartSessionAsync(string sessionId);
    
    /// <summary>
    /// Stops tracking.
    /// </summary>
    Task StopSessionAsync();
}
