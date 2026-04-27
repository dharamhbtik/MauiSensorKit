namespace MauiSensorKit.SampleApp.Services;

/// <summary>
/// Shared session state accessible across all ViewModels.
/// </summary>
public class SessionStateService
{
    private string? _currentSessionId;
    private bool _isRecording;
    private string _currentActivity = "Unknown";
    private double _currentBatteryLevel;
    private MauiSensorKit.RoutePoint? _lastKnownLocation;
    
    /// <summary>
    /// Current recording session ID.
    /// </summary>
    public string? CurrentSessionId
    {
        get => _currentSessionId;
        set
        {
            if (_currentSessionId != value)
            {
                _currentSessionId = value;
                SessionIdChanged?.Invoke(this, value);
            }
        }
    }
    
    /// <summary>
    /// Whether recording is currently active.
    /// </summary>
    public bool IsRecording
    {
        get => _isRecording;
        set
        {
            if (_isRecording != value)
            {
                _isRecording = value;
                RecordingStateChanged?.Invoke(this, value);
            }
        }
    }
    
    /// <summary>
    /// Current detected activity.
    /// </summary>
    public string CurrentActivity
    {
        get => _currentActivity;
        set
        {
            if (_currentActivity != value)
            {
                _currentActivity = value;
                ActivityChanged?.Invoke(this, value);
            }
        }
    }
    
    /// <summary>
    /// Current battery level (0.0-1.0).
    /// </summary>
    public double CurrentBatteryLevel
    {
        get => _currentBatteryLevel;
        set
        {
            if (Math.Abs(_currentBatteryLevel - value) > 0.001)
            {
                _currentBatteryLevel = value;
                BatteryLevelChanged?.Invoke(this, value);
            }
        }
    }
    
    /// <summary>
    /// Last known GPS location.
    /// </summary>
    public MauiSensorKit.RoutePoint? LastKnownLocation
    {
        get => _lastKnownLocation;
        set
        {
            _lastKnownLocation = value;
            if (value != null)
            {
                LocationUpdated?.Invoke(this, value);
            }
        }
    }
    
    /// <summary>
    /// Event raised when recording state changes.
    /// </summary>
    public event EventHandler<bool>? RecordingStateChanged;
    
    /// <summary>
    /// Event raised when activity changes.
    /// </summary>
    public event EventHandler<string>? ActivityChanged;
    
    /// <summary>
    /// Event raised when battery level changes.
    /// </summary>
    public event EventHandler<double>? BatteryLevelChanged;
    
    /// <summary>
    /// Event raised when location updates.
    /// </summary>
    public event EventHandler<MauiSensorKit.RoutePoint>? LocationUpdated;
    
    /// <summary>
    /// Event raised when session ID changes.
    /// </summary>
    public event EventHandler<string?>? SessionIdChanged;
}
