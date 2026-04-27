namespace MauiSensorKit.SampleApp.Views;

using MauiSensorKit.SampleApp.ViewModels;
using System.Text;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private bool _isMapInitialized;

    public MapPage(MapViewModel viewModel)
    {
        try
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;

            // Subscribe to viewmodel changes
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"MapPage constructor error: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        if (!_isMapInitialized)
        {
            InitializeMap();
            _isMapInitialized = true;
        }
        
        UpdateMapWithRoute();
    }
    
    private void InitializeMap()
    {
        try
        {
            var html = GenerateOpenStreetMapHtml();
            SensorMap.Source = new HtmlWebViewSource { Html = html };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Map initialization error: {ex.Message}");
        }
    }

    private string GenerateOpenStreetMapHtml()
    {
        var sb = new StringBuilder();
        sb.Append(@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no' />
    <title>OpenStreetMap</title>
    <link rel='stylesheet' href='https://unpkg.com/leaflet@1.9.4/dist/leaflet.css' />
    <script src='https://unpkg.com/leaflet@1.9.4/dist/leaflet.js'></script>
    <style>
        body { margin: 0; padding: 0; background: #0F0F1A; }
        #map { height: 100vh; width: 100vw; background: #0F0F1A; }
    </style>
</head>
<body>
    <div id='map'></div>
    <script>
        // Initialize map centered on world view
        var map = L.map('map', { zoomControl: false }).setView([20, 0], 2);
        
        // Add OpenStreetMap tile layer (free, no API key needed)
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '',
            maxZoom: 19
        }).addTo(map);
        
        // Store map reference for updates
        window.mapInstance = map;
        window.routeLayers = [];
        window.markers = [];
        
        // Function to clear route
        window.clearRoute = function() {
            window.routeLayers.forEach(function(layer) { map.removeLayer(layer); });
            window.markers.forEach(function(marker) { map.removeLayer(marker); });
            window.routeLayers = [];
            window.markers = [];
        };
        
        // Function to add route polyline
        window.addRoute = function(points, color) {
            if (points.length < 2) return;
            var latlngs = points.map(function(p) { return [p.lat, p.lng]; });
            var polyline = L.polyline(latlngs, { color: color, weight: 4, opacity: 0.8 }).addTo(map);
            window.routeLayers.push(polyline);
        };
        
        // Function to add marker
        window.addMarker = function(lat, lng, color) {
            var marker = L.circleMarker([lat, lng], {
                radius: 8,
                fillColor: color,
                color: '#fff',
                weight: 2,
                opacity: 1,
                fillOpacity: 0.8
            }).addTo(map);
            window.markers.push(marker);
        };
        
        // Function to center map
        window.centerOn = function(lat, lng, zoom) {
            map.setView([lat, lng], zoom);
        };
    </script>
</body>
</html>");
        return sb.ToString();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.CurrentPoints) || e.PropertyName == nameof(MapViewModel.CurrentLocation))
        {
            MainThread.BeginInvokeOnMainThread(UpdateMapWithRoute);
        }
    }

    private void UpdateMapWithRoute()
    {
        if (_viewModel.CurrentPoints.Count == 0 || !_isMapInitialized) return;

        try
        {
            // Build JavaScript to update map
            var js = new StringBuilder();
            js.Append("clearRoute();");
            
            // Group points by activity and draw segments
            string currentActivity = _viewModel.CurrentPoints[0].ActivityAtPoint;
            var currentSegment = new List<(double lat, double lng)>();
            
            for (int i = 0; i < _viewModel.CurrentPoints.Count; i++)
            {
                var point = _viewModel.CurrentPoints[i];
                
                if (point.ActivityAtPoint != currentActivity && currentSegment.Count > 1)
                {
                    // Draw segment with activity color
                    var color = GetActivityColorHex(currentActivity);
                    js.Append($"addRoute({SerializePoints(currentSegment)}, '{color}');");
                    currentSegment.Clear();
                    currentActivity = point.ActivityAtPoint;
                }
                
                currentSegment.Add((point.Latitude, point.Longitude));
            }
            
            // Draw final segment
            if (currentSegment.Count > 1)
            {
                var color = GetActivityColorHex(currentActivity);
                js.Append($"addRoute({SerializePoints(currentSegment)}, '{color}');");
            }
            
            // Add start marker
            if (_viewModel.CurrentPoints.Count > 0)
            {
                var start = _viewModel.CurrentPoints[0];
                js.Append($"addMarker({start.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {start.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '#00C896');");
            }
            
            // Center on current location if available
            if (_viewModel.CurrentLocation != null && _viewModel.IsAutoFollow)
            {
                js.Append($"centerOn({_viewModel.CurrentLocation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {_viewModel.CurrentLocation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 15);");
            }
            
            // Execute JavaScript
            SensorMap.EvaluateJavaScriptAsync(js.ToString());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Map update error: {ex.Message}");
        }
    }
    
    private string SerializePoints(List<(double lat, double lng)> points)
    {
        var items = points.Select(p => $"{{lat:{p.lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},lng:{p.lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
        return "[" + string.Join(",", items) + "]";
    }

    private string GetActivityColorHex(string activity) => activity switch
    {
        "Walking" or "Walk" => "#00C896",
        "Running" or "Run" => "#FF6B6B",
        "In Car/Bus" or "Driving" => "#6C63FF",
        "On Train/Metro" or "OnTrain" => "#00E5FF",
        "On Bus" => "#FF8C42",
        "On Stairs" or "Stairs" => "#FFB347",
        "In Lift/Escalator" or "Elevator" => "#C084FC",
        "Stationary" or "Standing" or "Sitting" => "#6B6B8A",
        _ => "#6C63FF"
    };

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
