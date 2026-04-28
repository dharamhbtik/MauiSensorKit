using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSensorKit;
using MauiSensorKit.SampleApp.Services;
using System.Collections.ObjectModel;

namespace MauiSensorKit.SampleApp.ViewModels;

/// <summary>
/// ViewModel for the live route map page.
/// </summary>
public partial class MapViewModel : ObservableObject, IDisposable
{
    private readonly RouteDataStore _routeDataStore;
    private readonly SessionStateService _sessionState;
    private readonly ISensorCollectionService _sensorService;
    
    [ObservableProperty]
    private bool _isTracking;
    
    [ObservableProperty]
    private bool _isAutoFollow = true;
    
    [ObservableProperty]
    private ObservableCollection<RoutePoint> _currentPoints = new();
    
    [ObservableProperty]
    private string _sessionId = string.Empty;
    
    [ObservableProperty]
    private string _currentActivity = "Unknown";
    
    [ObservableProperty]
    private double _totalDistanceKm;
    
    [ObservableProperty]
    private string _durationString = "00:00:00";
    
    [ObservableProperty]
    private double _currentSpeedMps;
    
    [ObservableProperty]
    private string _speedString = "0.0 km/h";
    
    [ObservableProperty]
    private int _pointCount;
    
    [ObservableProperty]
    private Microsoft.Maui.Devices.Sensors.Location? _currentLocation;
    
    [ObservableProperty]
    private int _currentZoom = 15;
    
    private System.Threading.Timer? _refreshTimer;
    private DateTime _sessionStartTime = DateTime.Now;
    private LocationPoint? _lastPoint;
    private bool _hasCenteredOnFirstLocation;
    private bool _needsInitialCenter = true;
    private bool _hasValidSessionStartTime = false;
    
    public MapViewModel(RouteDataStore routeDataStore, SessionStateService sessionState, ISensorCollectionService sensorService)
    {
        _routeDataStore = routeDataStore;
        _sessionState = sessionState;
        _sensorService = sensorService;
        
        // Subscribe to session state changes
        _sessionState.RecordingStateChanged += OnRecordingStateChanged;
        _sessionState.LocationUpdated += OnLocationUpdated;
        _sessionState.ActivityChanged += OnActivityChanged;
        
        // Subscribe to sensor readings for real-time location updates
        _sensorService.ReadingRecorded += OnReadingRecorded;
        
        _refreshTimer = new System.Threading.Timer(_ => UpdateStats(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        
        // Load initial state
        IsTracking = _sessionState.IsRecording;
        SessionId = _sessionState.CurrentSessionId ?? string.Empty;
        CurrentActivity = _sessionState.CurrentActivity;
        
        // If already recording, set session start time to now
        if (IsTracking)
        {
            _sessionStartTime = DateTime.Now;
            _hasValidSessionStartTime = true;
        }
    }
    
    private void OnRecordingStateChanged(object? sender, bool isRecording)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsTracking = isRecording;
            if (isRecording)
            {
                SessionId = _sessionState.CurrentSessionId ?? string.Empty;
                _sessionStartTime = DateTime.Now;
                _hasValidSessionStartTime = true;
                DurationString = "00:00:00";
                CurrentPoints.Clear();
                _lastPoint = null;
                _hasCenteredOnFirstLocation = false;
                _needsInitialCenter = true;
                TotalDistanceKm = 0;
            }
            else
            {
                _hasValidSessionStartTime = false;
            }
        });
    }
    
    private void OnLocationUpdated(object? sender, RoutePoint point)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentLocation = new Microsoft.Maui.Devices.Sensors.Location(point.Latitude, point.Longitude);
            
            // Auto-center on first location update
            if (_needsInitialCenter && IsAutoFollow)
            {
                _needsInitialCenter = false;
                CurrentZoom = 15;
            }
        });
    }
    
    private void OnActivityChanged(object? sender, string activity)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentActivity = activity;
        });
    }
    
    private void OnReadingRecorded(object? sender, SensorReading reading)
    {
        if (!IsTracking) return;
        
        if (reading is LocationReading loc)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var routePoint = new RoutePoint
                {
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude,
                    AltitudeMeters = loc.AltitudeMeters,
                    SpeedMps = loc.SpeedMps,
                    Timestamp = loc.Timestamp,
                    ActivityAtPoint = _sessionState.CurrentActivity
                };
                
                CurrentPoints.Add(routePoint);
                PointCount = CurrentPoints.Count;
                CurrentLocation = new Microsoft.Maui.Devices.Sensors.Location(loc.Latitude, loc.Longitude);
                CurrentSpeedMps = loc.SpeedMps ?? 0;
                SpeedString = $"{(loc.SpeedMps ?? 0) * 3.6:F1} km/h";
                
                // Auto-center on first location
                if (!_hasCenteredOnFirstLocation && IsAutoFollow)
                {
                    _hasCenteredOnFirstLocation = true;
                    CurrentZoom = 15;
                }
                
                // Calculate total distance
                if (_lastPoint != null)
                {
                    var distance = CalculateDistance(_lastPoint.Latitude, _lastPoint.Longitude, loc.Latitude, loc.Longitude);
                    TotalDistanceKm += distance;
                }
                
                _lastPoint = new LocationPoint
                {
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude,
                    Altitude = loc.AltitudeMeters,
                    Speed = loc.SpeedMps,
                    Timestamp = DateTime.UtcNow,
                    Accuracy = loc.AccuracyMeters
                };
            });
        }
    }
    
    private void UpdateStats()
    {
        if (!IsTracking || !_hasValidSessionStartTime) return;
        
        var duration = DateTime.Now - _sessionStartTime;
        // Sanity check - if duration is negative or unreasonably large, reset
        if (duration.TotalSeconds < 0 || duration.TotalHours > 24)
        {
            _sessionStartTime = DateTime.Now;
            DurationString = "00:00:00";
            return;
        }
        
        var totalSeconds = duration.TotalSeconds;
        var hours = (int)(totalSeconds / 3600);
        var minutes = (int)((totalSeconds % 3600) / 60);
        var seconds = (int)(totalSeconds % 60);
        DurationString = $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }
    
    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371; // Earth's radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
    
    private double ToRadians(double angle)
    {
        return angle * Math.PI / 180.0;
    }
    
    [RelayCommand]
    private void CenterOnCurrentLocation()
    {
        IsAutoFollow = true;
        CurrentZoom = 15;
        OnPropertyChanged(nameof(IsAutoFollow));
        OnPropertyChanged(nameof(CurrentZoom));
        
        // Trigger immediate center if we have location
        if (CurrentLocation != null)
        {
            OnPropertyChanged(nameof(CurrentLocation));
        }
    }
    
    [RelayCommand]
    private void ZoomIn()
    {
        if (CurrentZoom < 19)
        {
            CurrentZoom++;
            OnPropertyChanged(nameof(CurrentZoom));
        }
    }
    
    [RelayCommand]
    private void ZoomOut()
    {
        if (CurrentZoom > 1)
        {
            CurrentZoom--;
            OnPropertyChanged(nameof(CurrentZoom));
        }
    }
    
    [RelayCommand]
    private async Task ShareRouteAsync()
    {
        var text = $"Route: {TotalDistanceKm:F2} km in {DurationString}\n" +
                   $"Activity: {CurrentActivity}\n" +
                   $"Points: {PointCount}";
        
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = text,
            Title = "Share Route"
        });
    }
    
    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _sessionState.RecordingStateChanged -= OnRecordingStateChanged;
        _sessionState.LocationUpdated -= OnLocationUpdated;
        _sessionState.ActivityChanged -= OnActivityChanged;
        _sensorService.ReadingRecorded -= OnReadingRecorded;
    }
}
