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
    private readonly IRouteTrackingService _routeTrackingService;
    private readonly ISensorCollectionService _sensorService;
    private readonly SessionStateService _sessionState;
    
    [ObservableProperty]
    private bool _isTracking;
    
    [ObservableProperty]
    private bool _isAutoFollow = true;
    
    [ObservableProperty]
    private ObservableCollection<RoutePoint> _currentPoints = new();
    
    [ObservableProperty]
    private RouteSession? _currentSession;
    
    [ObservableProperty]
    private ObservableCollection<RouteSession> _pastSessions = new();
    
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
    
    private string _currentMapType = "Street";
    
    public string CurrentMapType
    {
        get => _currentMapType;
        set
        {
            if (_currentMapType != value)
            {
                _currentMapType = value;
                OnPropertyChanged(nameof(CurrentMapType));
            }
        }
    }
    
    [ObservableProperty]
    private bool _isReplayMode;
    
    [ObservableProperty]
    private RouteSession? _replaySession;
    
    [ObservableProperty]
    private Microsoft.Maui.Devices.Sensors.Location? _currentLocation;
    
    private System.Threading.Timer? _refreshTimer;
    
    public MapViewModel(IRouteTrackingService routeTrackingService, ISensorCollectionService sensorService, SessionStateService sessionState)
    {
        _routeTrackingService = routeTrackingService;
        _sensorService = sensorService;
        _sessionState = sessionState;
        
        _routeTrackingService.PointAdded += OnPointAdded;
        _routeTrackingService.SessionCompleted += OnSessionCompleted;
        
        _refreshTimer = new System.Threading.Timer(_ => UpdateStats(), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        
        _ = LoadPastSessionsAsync();
    }
    
    private void OnPointAdded(object? sender, RoutePoint point)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            CurrentPoints.Add(point);
            CurrentLocation = new Microsoft.Maui.Devices.Sensors.Location(point.Latitude, point.Longitude);
            CurrentSpeedMps = point.SpeedMps ?? 0;
            SpeedString = $"{(point.SpeedMps ?? 0) * 3.6:F1} km/h";
            CurrentActivity = point.ActivityAtPoint;
        });
    }
    
    private void OnSessionCompleted(object? sender, RouteSession session)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await LoadPastSessionsAsync();
        });
    }
    
    private void UpdateStats()
    {
        if (CurrentSession == null) return;
        
        var duration = DateTimeOffset.Now - CurrentSession.StartTime;
        DurationString = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        TotalDistanceKm = CurrentSession.TotalDistanceKm;
    }
    
    [RelayCommand]
    private async Task StartTrackingAsync()
    {
        var sessionId = Guid.NewGuid().ToString("N");
        await _routeTrackingService.StartTrackingAsync(sessionId);
        
        CurrentSession = _routeTrackingService.CurrentSession;
        IsTracking = true;
        CurrentPoints.Clear();
    }
    
    [RelayCommand]
    private async Task StopTrackingAsync()
    {
        await _routeTrackingService.StopTrackingAsync();
        IsTracking = false;
    }
    
    [RelayCommand]
    private async Task LoadPastSessionsAsync()
    {
        var sessions = await _routeTrackingService.GetAllSessionsAsync();
        
        PastSessions.Clear();
        foreach (var session in sessions)
        {
            PastSessions.Add(session);
        }
    }
    
    [RelayCommand]
    private void CenterOnCurrentLocation()
    {
        IsAutoFollow = true;
    }
    
    [RelayCommand]
    private void ToggleMapType()
    {
        CurrentMapType = CurrentMapType switch
        {
            "Street" => "Satellite",
            "Satellite" => "Hybrid",
            _ => "Street"
        };
    }
    
    [RelayCommand]
    private async Task ShareRouteAsync()
    {
        if (CurrentSession == null) return;
        
        var text = $"Route: {CurrentSession.TotalDistanceKm:F2} km in {CurrentSession.DurationString}\n" +
                   $"Activity: {CurrentSession.DominantActivity}";
        
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Text = text,
            Title = "Share Route"
        });
    }
    
    [RelayCommand]
    private async Task StartReplayAsync(RouteSession session)
    {
        IsReplayMode = true;
        ReplaySession = session;
        CurrentPoints = new ObservableCollection<RoutePoint>(session.Points);
        TotalDistanceKm = session.TotalDistanceKm;
        DurationString = session.DurationString;
        
        await Task.CompletedTask;
    }
    
    [RelayCommand]
    private void StopReplay()
    {
        IsReplayMode = false;
        ReplaySession = null;
        CurrentPoints.Clear();
        TotalDistanceKm = 0;
        DurationString = "00:00:00";
    }
    
    [RelayCommand]
    private async Task DeleteSessionAsync(RouteSession session)
    {
        await _routeTrackingService.DeleteSessionAsync(session.SessionId);
        await LoadPastSessionsAsync();
    }
    
    [RelayCommand]
    private void OnMapPan()
    {
        IsAutoFollow = false;
    }
    
    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _routeTrackingService.PointAdded -= OnPointAdded;
        _routeTrackingService.SessionCompleted -= OnSessionCompleted;
    }
}
