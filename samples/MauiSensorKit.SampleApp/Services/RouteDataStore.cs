using System.Collections.Concurrent;
using MauiSensorKit;

namespace MauiSensorKit.SampleApp.Services;

/// <summary>
/// Stores location readings for route tracking display.
/// </summary>
public sealed class RouteDataStore
{
    private readonly ConcurrentBag<LocationPoint> _points = new();
    private DateTime _sessionStartTime = DateTime.UtcNow;
    private string? _currentSessionId;

    public void StartNewSession(string sessionId)
    {
        _points.Clear();
        _currentSessionId = sessionId;
        _sessionStartTime = DateTime.UtcNow;
    }

    public void AddLocation(LocationReading reading)
    {
        if (reading.SessionId != _currentSessionId) return;
        
        _points.Add(new LocationPoint
        {
            Latitude = reading.Latitude,
            Longitude = reading.Longitude,
            Altitude = reading.AltitudeMeters,
            Speed = reading.SpeedMps,
            Timestamp = DateTime.UtcNow,
            Accuracy = reading.AccuracyMeters
        });
    }

    public IReadOnlyList<LocationPoint> GetPoints()
    {
        return _points.OrderBy(p => p.Timestamp).ToList();
    }

    public void Clear()
    {
        _points.Clear();
    }

    public TimeSpan GetSessionDuration()
    {
        return DateTime.UtcNow - _sessionStartTime;
    }
}

public sealed class LocationPoint
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Altitude { get; set; }
    public double? Speed { get; set; }
    public DateTime Timestamp { get; set; }
    public double? Accuracy { get; set; }
}
