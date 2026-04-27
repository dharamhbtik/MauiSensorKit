using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Service for tracking battery history and computing analytics.
/// </summary>
public class BatteryHistoryService : IBatteryHistoryService, IDisposable
{
    private readonly ILogger<BatteryHistoryService> _logger;
    private readonly List<BatterySnapshot> _currentSessionSnapshots = new();
    private readonly List<BatteryEvent> _currentSessionEvents = new();
    private BatterySnapshot? _lastSnapshot;
    private string? _currentSessionId;
    private readonly object _lock = new();
    private readonly string _storagePath;
    
    /// <summary>
    /// Event raised when a battery event is detected.
    /// </summary>
    public event EventHandler<BatteryEvent>? BatteryEventDetected;
    
    /// <summary>
    /// Gets the current session ID.
    /// </summary>
    public string? CurrentSessionId => _currentSessionId;
    
    /// <summary>
    /// Gets the last recorded snapshot.
    /// </summary>
    public BatterySnapshot? LastSnapshot => _lastSnapshot;
    
    /// <summary>
    /// Creates a new battery history service.
    /// </summary>
    public BatteryHistoryService(ILogger<BatteryHistoryService> logger)
    {
        _logger = logger;
        _storagePath = Path.Combine(FileSystem.AppDataDirectory, "battery_history");
        Directory.CreateDirectory(_storagePath);
    }
    
    /// <summary>
    /// Starts a new tracking session.
    /// </summary>
    public Task StartSessionAsync(string sessionId)
    {
        lock (_lock)
        {
            _currentSessionId = sessionId;
            _currentSessionSnapshots.Clear();
            _currentSessionEvents.Clear();
            _lastSnapshot = null;
        }
        
        _logger.LogInformation("Started battery history session: {SessionId}", sessionId);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Stops the current session.
    /// </summary>
    public async Task StopSessionAsync()
    {
        if (_currentSessionId == null) return;
        
        // Persist the session data
        await PersistSessionAsync(_currentSessionId);
        
        lock (_lock)
        {
            _currentSessionId = null;
            _currentSessionSnapshots.Clear();
            _currentSessionEvents.Clear();
        }
        
        _logger.LogInformation("Stopped battery history session");
    }
    
    /// <summary>
    /// Records a battery snapshot.
    /// </summary>
    public Task RecordSnapshotAsync(BatterySnapshot snapshot)
    {
        lock (_lock)
        {
            // Detect events by comparing with previous snapshot
            DetectEvents(snapshot);
            
            _currentSessionSnapshots.Add(snapshot);
            _lastSnapshot = snapshot;
            
            // Trim if too many (keep last 500)
            if (_currentSessionSnapshots.Count > 500)
            {
                _currentSessionSnapshots.RemoveAt(0);
            }
        }
        
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Detects battery events by comparing with previous snapshot.
    /// </summary>
    private void DetectEvents(BatterySnapshot current)
    {
        if (_lastSnapshot == null) return;
        
        var events = new List<BatteryEvent>();
        var sessionId = _currentSessionId ?? "unknown";
        
        // Charging started
        if (current.State == BatteryState.Charging && _lastSnapshot.State != BatteryState.Charging)
        {
            events.Add(new BatteryEvent
            {
                Timestamp = current.Timestamp,
                Type = BatteryEventType.ChargingStarted,
                Description = "Charging started",
                ChargeAtEvent = current.ChargeLevel,
                SessionId = sessionId
            });
        }
        
        // Charging stopped
        if (current.State != BatteryState.Charging && _lastSnapshot.State == BatteryState.Charging)
        {
            events.Add(new BatteryEvent
            {
                Timestamp = current.Timestamp,
                Type = BatteryEventType.ChargingStopped,
                Description = "Charging stopped",
                ChargeAtEvent = current.ChargeLevel,
                SessionId = sessionId
            });
        }
        
        // Charging completed (reached 100%)
        if (current.ChargeLevel >= 1.0 && _lastSnapshot.ChargeLevel < 1.0)
        {
            events.Add(new BatteryEvent
            {
                Timestamp = current.Timestamp,
                Type = BatteryEventType.FullyCharged,
                Description = "Battery fully charged",
                ChargeAtEvent = 1.0,
                SessionId = sessionId
            });
        }
        
        // Low battery warning (< 20%)
        if (current.ChargeLevel < 0.20 && _lastSnapshot.ChargeLevel >= 0.20)
        {
            events.Add(new BatteryEvent
            {
                Timestamp = current.Timestamp,
                Type = BatteryEventType.LowBatteryWarning,
                Description = "Low battery warning",
                ChargeAtEvent = current.ChargeLevel,
                SessionId = sessionId
            });
        }
        
        // Critical battery (< 10%)
        if (current.ChargeLevel < 0.10 && _lastSnapshot.ChargeLevel >= 0.10)
        {
            events.Add(new BatteryEvent
            {
                Timestamp = current.Timestamp,
                Type = BatteryEventType.CriticalBattery,
                Description = "Critical battery level",
                ChargeAtEvent = current.ChargeLevel,
                SessionId = sessionId
            });
        }
        
        // Overheat warning (> 45°C)
        if (current.TemperatureCelsius > 45 && (_lastSnapshot.TemperatureCelsius ?? 0) <= 45)
        {
            events.Add(new BatteryEvent
            {
                Timestamp = current.Timestamp,
                Type = BatteryEventType.OverheatWarning,
                Description = "Battery overheating",
                ChargeAtEvent = current.ChargeLevel,
                SessionId = sessionId
            });
        }
        
        // Power source changed
        if (current.PowerSource != _lastSnapshot.PowerSource)
        {
            events.Add(new BatteryEvent
            {
                Timestamp = current.Timestamp,
                Type = BatteryEventType.PowerSourceChanged,
                Description = $"Power source changed to {current.PowerSource}",
                ChargeAtEvent = current.ChargeLevel,
                SessionId = sessionId
            });
        }
        
        foreach (var evt in events)
        {
            _currentSessionEvents.Add(evt);
            BatteryEventDetected?.Invoke(this, evt);
        }
    }
    
    /// <summary>
    /// Gets all snapshots for a session.
    /// </summary>
    public async Task<List<BatterySnapshot>> GetSessionHistoryAsync(string sessionId)
    {
        // If requesting current session, return from memory
        if (sessionId == _currentSessionId)
        {
            lock (_lock)
            {
                return new List<BatterySnapshot>(_currentSessionSnapshots);
            }
        }
        
        // Load from disk
        var filePath = GetSessionFilePath(sessionId);
        if (!File.Exists(filePath)) return new List<BatterySnapshot>();
        
        var json = await File.ReadAllTextAsync(filePath);
        var sessionData = JsonSerializer.Deserialize<SessionData>(json);
        return sessionData?.Snapshots ?? new List<BatterySnapshot>();
    }
    
    /// <summary>
    /// Gets snapshots within a date range.
    /// </summary>
    public async Task<List<BatterySnapshot>> GetHistoryRangeAsync(DateTimeOffset from, DateTimeOffset to)
    {
        var allSnapshots = new List<BatterySnapshot>();
        
        // Get all session files
        var files = Directory.GetFiles(_storagePath, "battery_*.json");
        foreach (var file in files)
        {
            var json = await File.ReadAllTextAsync(file);
            var sessionData = JsonSerializer.Deserialize<SessionData>(json);
            if (sessionData?.Snapshots != null)
            {
                allSnapshots.AddRange(sessionData.Snapshots.Where(s => 
                    s.Timestamp >= from && s.Timestamp <= to));
            }
        }
        
        return allSnapshots.OrderBy(s => s.Timestamp).ToList();
    }
    
    /// <summary>
    /// Computes analytics for a session.
    /// </summary>
    public async Task<BatteryAnalytics> ComputeAnalyticsAsync(string sessionId)
    {
        var snapshots = await GetSessionHistoryAsync(sessionId);
        var events = await GetEventsAsync(sessionId);
        
        return ComputeAnalyticsInternal(snapshots, events, sessionId);
    }
    
    /// <summary>
    /// Computes analytics for a date range.
    /// </summary>
    public async Task<BatteryAnalytics> ComputeAnalyticsRangeAsync(DateTimeOffset from, DateTimeOffset to)
    {
        var snapshots = await GetHistoryRangeAsync(from, to);
        
        return ComputeAnalyticsInternal(snapshots, new List<BatteryEvent>(), "range");
    }
    
    /// <summary>
    /// Internal method to compute analytics from snapshots.
    /// </summary>
    private BatteryAnalytics ComputeAnalyticsInternal(List<BatterySnapshot> snapshots, List<BatteryEvent> events, string sessionId)
    {
        if (snapshots.Count < 2)
        {
            return new BatteryAnalytics 
            { 
                SessionId = sessionId,
                Events = events 
            };
        }
        
        var ordered = snapshots.OrderBy(s => s.Timestamp).ToList();
        var start = ordered.First();
        var end = ordered.Last();
        var duration = end.Timestamp - start.Timestamp;
        var durationHours = duration.TotalHours;
        
        // Discharge rate
        var chargeChange = start.ChargeLevel - end.ChargeLevel;
        var dischargeRate = durationHours > 0 ? chargeChange * 100 / durationHours : 0;
        
        // Estimated full drain
        var currentCharge = end.ChargeLevel;
        var estimatedDrainMinutes = dischargeRate > 0 
            ? (currentCharge / (dischargeRate / 100)) * 60 
            : (double?)null;
        
        // Temperature stats
        var temps = ordered.Where(s => s.TemperatureCelsius.HasValue).Select(s => s.TemperatureCelsius.Value).ToList();
        var avgTemp = temps.Count > 0 ? temps.Average() : (double?)null;
        var peakTemp = temps.Count > 0 ? temps.Max() : (double?)null;
        
        // Time spent in each state
        var chargingTime = TimeSpan.Zero;
        var dischargingTime = TimeSpan.Zero;
        for (int i = 1; i < ordered.Count; i++)
        {
            var timeDelta = ordered[i].Timestamp - ordered[i-1].Timestamp;
            if (ordered[i-1].State == BatteryState.Charging)
                chargingTime += timeDelta;
            else
                dischargingTime += timeDelta;
        }
        
        // Charging cycles
        var chargingCycles = events.Count(e => e.Type == BatteryEventType.ChargingStarted);
        
        return new BatteryAnalytics
        {
            SessionId = sessionId,
            AverageDischargeRatePerHour = Math.Max(0, dischargeRate),
            EstimatedFullDrainMinutes = estimatedDrainMinutes,
            PeakTemperatureCelsius = peakTemp,
            AverageTemperatureCelsius = avgTemp,
            TotalChargingTime = chargingTime,
            TotalDischargingTime = dischargingTime,
            ChargingCyclesInSession = chargingCycles,
            LowestChargeLevel = ordered.Min(s => s.ChargeLevel),
            HighestChargeLevel = ordered.Max(s => s.ChargeLevel),
            Events = events,
            SessionStart = start.Timestamp,
            SessionEnd = end.Timestamp
        };
    }
    
    /// <summary>
    /// Gets events for a session.
    /// </summary>
    public async Task<List<BatteryEvent>> GetEventsAsync(string sessionId)
    {
        // If requesting current session, return from memory
        if (sessionId == _currentSessionId)
        {
            lock (_lock)
            {
                return new List<BatteryEvent>(_currentSessionEvents);
            }
        }
        
        // Load from disk
        var filePath = GetSessionFilePath(sessionId);
        if (!File.Exists(filePath)) return new List<BatteryEvent>();
        
        var json = await File.ReadAllTextAsync(filePath);
        var sessionData = JsonSerializer.Deserialize<SessionData>(json);
        return sessionData?.Events ?? new List<BatteryEvent>();
    }
    
    /// <summary>
    /// Persists the current session to disk.
    /// </summary>
    private async Task PersistSessionAsync(string sessionId)
    {
        lock (_lock)
        {
            var sessionData = new SessionData
            {
                SessionId = sessionId,
                Snapshots = new List<BatterySnapshot>(_currentSessionSnapshots),
                Events = new List<BatteryEvent>(_currentSessionEvents)
            };
            
            var filePath = GetSessionFilePath(sessionId);
            var json = JsonSerializer.Serialize(sessionData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// Gets the file path for a session.
    /// </summary>
    private string GetSessionFilePath(string sessionId)
    {
        return Path.Combine(_storagePath, $"battery_{sessionId}.json");
    }
    
    /// <summary>
    /// Disposes the service.
    /// </summary>
    public void Dispose()
    {
        if (_currentSessionId != null)
        {
            _ = StopSessionAsync();
        }
    }
    
    /// <summary>
    /// Session data for persistence.
    /// </summary>
    private class SessionData
    {
        public string SessionId { get; set; } = string.Empty;
        public List<BatterySnapshot> Snapshots { get; set; } = new();
        public List<BatteryEvent> Events { get; set; } = new();
    }
}
