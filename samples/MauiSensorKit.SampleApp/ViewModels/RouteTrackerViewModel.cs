using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSensorKit.SampleApp.Services;
using System.Collections.ObjectModel;

namespace MauiSensorKit.SampleApp.ViewModels;

/// <summary>
/// ViewModel for the route tracker page.
/// </summary>
public partial class RouteTrackerViewModel : ObservableObject
{
    private readonly RouteDataStore _routeDataStore;
    private readonly ISensorCollectionService _sensorService;

    [ObservableProperty]
    private string _mapHtml = string.Empty;

    [ObservableProperty]
    private bool _hasRouteData;

    [ObservableProperty]
    private int _pointCount;

    [ObservableProperty]
    private string _sessionDuration = "0:00";

    [ObservableProperty]
    private double _totalDistanceKm;

    [ObservableProperty]
    private string _currentStatus = "No active recording";

    private System.Threading.Timer? _updateTimer;

    public RouteTrackerViewModel(RouteDataStore routeDataStore, ISensorCollectionService sensorService)
    {
        _routeDataStore = routeDataStore;
        _sensorService = sensorService;

        GenerateMapHtml();
        
        // Check if recording is already running (started from Dashboard)
        if (_sensorService.IsRunning)
        {
            CurrentStatus = "Recording active - updating route...";
        }
        
        _updateTimer = new System.Threading.Timer(_ => UpdateStats(), null, TimeSpan.Zero, TimeSpan.FromSeconds(2));
    }

    [RelayCommand]
    private void RefreshMap()
    {
        GenerateMapHtml();
        UpdateStats();
    }

    [RelayCommand]
    private void ClearRoute()
    {
        _routeDataStore.Clear();
        GenerateMapHtml();
        UpdateStats();
    }

    private void GenerateMapHtml()
    {
        var points = _routeDataStore.GetPoints();
        
        if (points.Count == 0)
        {
            MapHtml = GetEmptyMapHtml();
            HasRouteData = false;
            return;
        }

        HasRouteData = true;
        MapHtml = GenerateSvgMap(points);
    }

    private static string GenerateSvgMap(IReadOnlyList<LocationPoint> points)
    {
        // Calculate bounds
        var minLat = points.Min(p => p.Latitude);
        var maxLat = points.Max(p => p.Latitude);
        var minLng = points.Min(p => p.Longitude);
        var maxLng = points.Max(p => p.Longitude);
        
        // Add padding
        var latPadding = (maxLat - minLat) * 0.1;
        var lngPadding = (maxLng - minLng) * 0.1;
        minLat -= latPadding; maxLat += latPadding;
        minLng -= lngPadding; maxLng += lngPadding;
        
        // Ensure minimum zoom
        if (maxLat - minLat < 0.001) { minLat -= 0.005; maxLat += 0.005; }
        if (maxLng - minLng < 0.001) { minLng -= 0.005; maxLng += 0.005; }

        // SVG dimensions
        const int width = 800;
        const int height = 600;

        // Scale functions
        double ScaleX(double lng) => (lng - minLng) / (maxLng - minLng) * width;
        double ScaleY(double lat) => height - (lat - minLat) / (maxLat - minLat) * height;

        // Build path
        var pathBuilder = new System.Text.StringBuilder();
        pathBuilder.Append($"M {ScaleX(points[0].Longitude):F1},{ScaleY(points[0].Latitude):F1}");
        for (int i = 1; i < points.Count; i++)
        {
            pathBuilder.Append($" L {ScaleX(points[i].Longitude):F1},{ScaleY(points[i].Latitude):F1}");
        }

        // Build markers
        var start = points[0];
        var end = points[points.Count - 1];
        
        var svg = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <style>
        body {{ margin: 0; padding: 0; background: #f0f0f0; font-family: sans-serif; }}
        #map-container {{ width: 100%; height: 100vh; display: flex; align-items: center; justify-content: center; }}
        svg {{ background: white; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .marker {{ font-size: 14px; font-weight: bold; }}
    </style>
</head>
<body>
    <div id='map-container'>
        <svg width='{width}' height='{height}' viewBox='0 0 {width} {height}' xmlns='http://www.w3.org/2000/svg'>
            <!-- Grid -->
            <defs>
                <pattern id='grid' width='50' height='50' patternUnits='userSpaceOnUse'>
                    <path d='M 50 0 L 0 0 0 50' fill='none' stroke='#e0e0e0' stroke-width='1'/>
                </pattern>
            </defs>
            <rect width='100%' height='100%' fill='url(#grid)' />
            
            <!-- Route path -->
            <path d='{pathBuilder}' fill='none' stroke='#512BD4' stroke-width='3' stroke-linecap='round' stroke-linejoin='round' />
            
            <!-- Start marker (green) -->
            <circle cx='{ScaleX(start.Longitude):F1}' cy='{ScaleY(start.Latitude):F1}' r='8' fill='#4CAF50' stroke='white' stroke-width='2' />
            <text x='{ScaleX(start.Longitude):F1}' y='{ScaleY(start.Latitude):F1 - 15:F1}' text-anchor='middle' class='marker' fill='#4CAF50'>START</text>
            
            <!-- End marker (red) -->
            <circle cx='{ScaleX(end.Longitude):F1}' cy='{ScaleY(end.Latitude):F1}' r='8' fill='#F44336' stroke='white' stroke-width='2' />
            <text x='{ScaleX(end.Longitude):F1}' y='{ScaleY(end.Latitude):F1 - 15:F1}' text-anchor='middle' class='marker' fill='#F44336'>CURRENT</text>
            
            <!-- Intermediate points -->
            {string.Join("", points.Skip(1).Take(points.Count - 2).Select((p, i) => 
                $"<circle cx='{ScaleX(p.Longitude):F1}' cy='{ScaleY(p.Latitude):F1}' r='3' fill='#512BD4' />"))}
        </svg>
    </div>
</body>
</html>";
        return svg;
    }

    private static string GetEmptyMapHtml()
    {
        return @"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <style>
        body { margin: 0; padding: 0; background: #f5f5f5; font-family: sans-serif; }
        .message { 
            position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); 
            background: white; padding: 30px; border-radius: 15px; 
            box-shadow: 0 4px 20px rgba(0,0,0,0.15); text-align: center;
        }
        h3 { color: #333; margin-bottom: 10px; }
        p { color: #666; }
    </style>
</head>
<body>
    <div class='message'>
        <h3>No route data</h3>
        <p>Start recording to track your route.<br>GPS points will appear here.</p>
    </div>
</body>
</html>";
    }

    private void UpdateStats()
    {
        var points = _routeDataStore.GetPoints();
        PointCount = points.Count;

        if (_sensorService.IsRunning)
        {
            var duration = _routeDataStore.GetSessionDuration();
            SessionDuration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            CurrentStatus = "Recording active";
        }
        else
        {
            CurrentStatus = points.Count > 0 ? "Recording stopped" : "No active recording";
        }

        TotalDistanceKm = CalculateTotalDistance(points);
    }

    private static double CalculateTotalDistance(IReadOnlyList<LocationPoint> points)
    {
        if (points.Count < 2) return 0;
        
        double totalDistance = 0;
        for (int i = 1; i < points.Count; i++)
        {
            totalDistance += CalculateDistance(
                points[i - 1].Latitude, points[i - 1].Longitude,
                points[i].Latitude, points[i].Longitude);
        }
        return totalDistance;
    }

    private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth's radius in km
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;

    public void Dispose()
    {
        _updateTimer?.Dispose();
    }
}
