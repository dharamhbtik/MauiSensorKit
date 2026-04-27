using System.Collections.Concurrent;
using MauiSensorKit;
using Microsoft.Maui.Devices;

namespace MauiSensorKit.SampleApp.Services;

/// <summary>
/// Stores battery readings for graph display.
/// </summary>
public sealed class BatteryDataStore
{
    private readonly ConcurrentBag<BatteryReadingPoint> _readings = new();
    private DateTime _sessionStartTime = DateTime.UtcNow;
    private string? _currentSessionId;
    private const int MaxReadings = 500; // Keep last 500 readings to prevent memory issues

    public void StartNewSession(string sessionId)
    {
        _readings.Clear();
        _currentSessionId = sessionId;
        _sessionStartTime = DateTime.UtcNow;
    }

    public void AddReading(BatteryReading reading)
    {
        if (reading.SessionId != _currentSessionId) return;
        
        _readings.Add(new BatteryReadingPoint
        {
            Percentage = reading.ChargeLevel * 100, // Convert 0.0-1.0 to 0-100
            State = reading.State.ToString(),
            Timestamp = DateTime.UtcNow,
            PowerSource = reading.PowerSource.ToString()
        });

        // Trim old readings if we exceed max
        if (_readings.Count > MaxReadings)
        {
            var sorted = _readings.OrderBy(r => r.Timestamp).ToList();
            _readings.Clear();
            foreach (var r in sorted.Skip(sorted.Count - MaxReadings))
            {
                _readings.Add(r);
            }
        }
    }

    public IReadOnlyList<BatteryReadingPoint> GetReadings()
    {
        return _readings.OrderBy(r => r.Timestamp).ToList();
    }

    public void Clear()
    {
        _readings.Clear();
    }

    public TimeSpan GetSessionDuration()
    {
        return DateTime.UtcNow - _sessionStartTime;
    }

    /// <summary>
    /// Gets the current battery reading directly from the device.
    /// This works even when not recording.
    /// </summary>
    public BatteryReadingPoint? GetCurrentReading()
    {
        try
        {
            var chargeLevel = Battery.ChargeLevel;
            var state = Battery.State;
            var powerSource = Battery.PowerSource;
            
            System.Diagnostics.Debug.WriteLine($"Battery reading: {chargeLevel * 100:F0}%, State={state}, Power={powerSource}");
            
            return new BatteryReadingPoint
            {
                Percentage = chargeLevel * 100,
                State = state.ToString(),
                Timestamp = DateTime.UtcNow,
                PowerSource = powerSource.ToString()
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Battery reading error: {ex.Message}");
            return null;
        }
    }
}

public sealed class BatteryReadingPoint
{
    public double Percentage { get; set; }
    public string State { get; set; } = "Unknown";
    public DateTime Timestamp { get; set; }
    public string PowerSource { get; set; } = "Unknown";
}
