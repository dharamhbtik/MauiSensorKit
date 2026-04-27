using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Service for tracking GPS routes with activity context.
/// </summary>
public class RouteTrackingService : IRouteTrackingService, IDisposable
{
    private readonly ILogger<RouteTrackingService> _logger;
    private readonly ISensorCollectionService _sensorService;
    private RouteSession? _currentSession;
    private RoutePoint? _lastPoint;
    private readonly List<RoutePoint> _points = new();
    private string? _currentActivity = "Unknown";
    private double? _currentBatteryLevel;
    private readonly string _storagePath;
    private bool _isTracking;
    
    /// <summary>
    /// Event raised when a new point is added.
    /// </summary>
    public event EventHandler<RoutePoint>? PointAdded;
    
    /// <summary>
    /// Event raised when a session is completed.
    /// </summary>
    public event EventHandler<RouteSession>? SessionCompleted;
    
    /// <summary>
    /// Gets whether tracking is active.
    /// </summary>
    public bool IsTracking => _isTracking;
    
    /// <summary>
    /// Gets the current session.
    /// </summary>
    public RouteSession? CurrentSession => _currentSession;
    
    /// <summary>
    /// Gets all points in the current session.
    /// </summary>
    public List<RoutePoint> CurrentPoints => new(_points);
    
    /// <summary>
    /// Gets the current activity.
    /// </summary>
    public string CurrentActivity => _currentActivity ?? "Unknown";
    
    /// <summary>
    /// Creates a new route tracking service.
    /// </summary>
    public RouteTrackingService(
        ISensorCollectionService sensorService,
        ILogger<RouteTrackingService> logger)
    {
        _sensorService = sensorService;
        _logger = logger;
        _storagePath = Path.Combine(FileSystem.AppDataDirectory, "routes");
        Directory.CreateDirectory(_storagePath);
        
        // Subscribe to sensor readings
        _sensorService.ReadingRecorded += OnSensorReading;
    }
    
    /// <summary>
    /// Starts a new tracking session.
    /// </summary>
    public Task StartTrackingAsync(string sessionId, CancellationToken ct = default)
    {
        _currentSession = new RouteSession
        {
            SessionId = sessionId,
            StartTime = DateTimeOffset.Now,
            Points = new List<RoutePoint>()
        };
        
        _points.Clear();
        _lastPoint = null;
        _isTracking = true;
        
        _logger.LogInformation("Started route tracking session: {SessionId}", sessionId);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Stops tracking.
    /// </summary>
    public async Task StopTrackingAsync()
    {
        if (!_isTracking || _currentSession == null) return;
        
        _isTracking = false;
        _currentSession.EndTime = DateTimeOffset.Now;
        _currentSession.Points = new List<RoutePoint>(_points);
        _currentSession.TotalDistanceMeters = ComputeTotalDistance();
        _currentSession.DominantActivity = ComputeDominantActivity();
        
        if (_points.Count > 0)
        {
            _currentSession.MaxSpeedMps = _points.Max(p => p.SpeedMps ?? 0);
            _currentSession.AverageSpeedMps = _points.Average(p => p.SpeedMps ?? 0);
            _currentSession.TotalAltitudeGainMeters = ComputeAltitudeGain();
        }
        
        // Persist session
        await PersistSessionAsync(_currentSession);
        
        SessionCompleted?.Invoke(this, _currentSession);
        
        _logger.LogInformation(
            "Stopped route tracking. Distance: {Distance:F2}km, Points: {Points}, Duration: {Duration}",
            _currentSession.TotalDistanceKm,
            _points.Count,
            _currentSession.DurationString);
    }
    
    /// <summary>
    /// Handles sensor readings.
    /// </summary>
    private void OnSensorReading(object? sender, SensorReading reading)
    {
        if (!_isTracking) return;
        
        switch (reading)
        {
            case LocationReading loc:
                AddLocationPoint(loc);
                break;
            case BatteryReading battery:
                _currentBatteryLevel = battery.ChargeLevel;
                break;
        }
    }
    
    /// <summary>
    /// Adds a location point to the route.
    /// </summary>
    private void AddLocationPoint(LocationReading location)
    {
        // Skip if accuracy is too poor
        if (location.AccuracyMeters > 50) return;
        
        var point = new RoutePoint
        {
            Timestamp = location.Timestamp,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            AltitudeMeters = location.AltitudeMeters,
            AccuracyMeters = location.AccuracyMeters,
            SpeedMps = location.SpeedMps,
            CourseDegrees = location.CourseDegrees,
            SessionId = _currentSession?.SessionId ?? string.Empty,
            ActivityAtPoint = _currentActivity ?? "Unknown",
            BatteryLevelAtPoint = _currentBatteryLevel
        };
        
        // Filter: skip if too close to last point (noise reduction)
        if (_lastPoint != null)
        {
            var distance = ComputeDistanceMeters(_lastPoint, point);
            if (distance < 5) return; // Skip points closer than 5 meters
        }
        
        _points.Add(point);
        _lastPoint = point;
        
        PointAdded?.Invoke(this, point);
    }
    
    /// <summary>
    /// Computes distance between two points using Haversine formula.
    /// </summary>
    public double ComputeDistanceMeters(RoutePoint p1, RoutePoint p2)
    {
        return ComputeDistanceMeters(p1.Latitude, p1.Longitude, p2.Latitude, p2.Longitude);
    }
    
    /// <summary>
    /// Computes distance using Haversine formula.
    /// </summary>
    public double ComputeDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth radius in meters
        
        var latRad1 = ToRadians(lat1);
        var latRad2 = ToRadians(lat2);
        var deltaLat = ToRadians(lat2 - lat1);
        var deltaLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(latRad1) * Math.Cos(latRad2) *
                Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return R * c;
    }
    
    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
    
    /// <summary>
    /// Computes total distance of the route.
    /// </summary>
    private double ComputeTotalDistance()
    {
        if (_points.Count < 2) return 0;
        
        double total = 0;
        for (int i = 1; i < _points.Count; i++)
        {
            total += ComputeDistanceMeters(_points[i - 1], _points[i]);
        }
        return total;
    }
    
    /// <summary>
    /// Computes altitude gain.
    /// </summary>
    private double? ComputeAltitudeGain()
    {
        var altitudes = _points.Where(p => p.AltitudeMeters.HasValue).Select(p => p.AltitudeMeters.Value).ToList();
        if (altitudes.Count < 2) return null;
        
        double gain = 0;
        for (int i = 1; i < altitudes.Count; i++)
        {
            var delta = altitudes[i] - altitudes[i - 1];
            if (delta > 0) gain += delta;
        }
        return gain;
    }
    
    /// <summary>
    /// Computes the dominant activity.
    /// </summary>
    private string ComputeDominantActivity()
    {
        if (_points.Count == 0) return "Unknown";
        
        return _points
            .GroupBy(p => p.ActivityAtPoint)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key ?? "Unknown";
    }
    
    /// <summary>
    /// Gets all stored sessions.
    /// </summary>
    public async Task<List<RouteSession>> GetAllSessionsAsync()
    {
        var sessions = new List<RouteSession>();
        var files = Directory.GetFiles(_storagePath, "route_*.json");
        
        foreach (var file in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var session = JsonSerializer.Deserialize<RouteSession>(json);
                if (session != null) sessions.Add(session);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load route session from {File}", file);
            }
        }
        
        return sessions.OrderByDescending(s => s.StartTime).ToList();
    }
    
    /// <summary>
    /// Gets a specific session.
    /// </summary>
    public async Task<RouteSession?> GetSessionAsync(string sessionId)
    {
        // Check if it's the current session
        if (_currentSession?.SessionId == sessionId)
        {
            return _currentSession;
        }
        
        var filePath = GetSessionFilePath(sessionId);
        if (!File.Exists(filePath)) return null;
        
        var json = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<RouteSession>(json);
    }
    
    /// <summary>
    /// Deletes a session.
    /// </summary>
    public Task DeleteSessionAsync(string sessionId)
    {
        var filePath = GetSessionFilePath(sessionId);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Persists a session to disk.
    /// </summary>
    private async Task PersistSessionAsync(RouteSession session)
    {
        var filePath = GetSessionFilePath(session.SessionId);
        var json = JsonSerializer.Serialize(session, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);
    }
    
    /// <summary>
    /// Gets the file path for a session.
    /// </summary>
    private string GetSessionFilePath(string sessionId)
    {
        return Path.Combine(_storagePath, $"route_{sessionId}.json");
    }
    
    /// <summary>
    /// Disposes the service.
    /// </summary>
    public void Dispose()
    {
        _sensorService.ReadingRecorded -= OnSensorReading;
        if (_isTracking)
        {
            _ = StopTrackingAsync();
        }
    }
}
