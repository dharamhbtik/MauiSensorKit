namespace MauiSensorKit.SampleApp.Views;

using MauiSensorKit.SampleApp.ViewModels;
using System.Text;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private bool _isMapInitialized;

    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Subscribe to viewmodel changes
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        
        // Wire up button click handlers
        ZoomInButton.Clicked += OnZoomInClicked;
        ZoomOutButton.Clicked += OnZoomOutClicked;
        CenterButton.Clicked += OnCenterClicked;
    }

    private async void OnZoomInClicked(object? sender, EventArgs e)
    {
        _viewModel.ZoomInCommand.Execute(null);
        await ExecuteMapZoom();
    }

    private async void OnZoomOutClicked(object? sender, EventArgs e)
    {
        _viewModel.ZoomOutCommand.Execute(null);
        await ExecuteMapZoom();
    }

    private async void OnCenterClicked(object? sender, EventArgs e)
    {
        _viewModel.CenterOnCurrentLocationCommand.Execute(null);
        await CenterMapOnCurrentLocation();
    }

    private async Task ExecuteMapZoom()
    {
        if (!_isMapInitialized) return;
        
        try
        {
            // Get location from ViewModel or fetch directly
            var location = _viewModel.CurrentLocation;
            if (location == null)
            {
                location = await Geolocation.GetLocationAsync(new GeolocationRequest
                {
                    DesiredAccuracy = GeolocationAccuracy.Medium,
                    Timeout = TimeSpan.FromSeconds(5)
                });
            }
            
            if (location != null)
            {
                var js = $"centerOn({location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {_viewModel.CurrentZoom});";
                await SensorMap.EvaluateJavaScriptAsync(js);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Zoom error: {ex.Message}");
        }
    }

    private async Task CenterMapOnCurrentLocation()
    {
        if (!_isMapInitialized) return;
        
        try
        {
            // Try to get current location from ViewModel first
            if (_viewModel.CurrentLocation != null)
            {
                var js = $"centerOn({_viewModel.CurrentLocation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {_viewModel.CurrentLocation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 15);";
                await SensorMap.EvaluateJavaScriptAsync(js);
                return;
            }
            
            // Otherwise try to get GPS location directly
            var location = await Geolocation.GetLocationAsync(new GeolocationRequest
            {
                DesiredAccuracy = GeolocationAccuracy.Medium,
                Timeout = TimeSpan.FromSeconds(5)
            });
            
            if (location != null)
            {
                var js = $"centerOn({location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 15);";
                await SensorMap.EvaluateJavaScriptAsync(js);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Center map error: {ex.Message}");
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Delay map initialization to ensure WebView is ready
        if (!_isMapInitialized)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500);
                InitializeMap();
                _isMapInitialized = true;
                
                // Wait for map to be ready then center on location
                await Task.Delay(1000);
                await CenterMapOnCurrentLocation();
            });
        }
        else
        {
            // Map already initialized, just center on location
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(500);
                await CenterMapOnCurrentLocation();
            });
        }
        
        // Delay route update to ensure map is initialized
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(1500);
            UpdateMapWithRoute();
        });
    }
    
    private void InitializeMap()
    {
        try
        {
            if (SensorMap == null)
            {
                System.Diagnostics.Debug.WriteLine("SensorMap is null");
                return;
            }
            
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
        var map = L.map('map', { zoomControl: false }).setView([20, 0], 2);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '',
            maxZoom: 19
        }).addTo(map);
        window.mapInstance = map;
        window.routeLayers = [];
        window.markers = [];
        window.currentLocationMarker = null;
        window.clearRoute = function() {
            window.routeLayers.forEach(function(layer) { map.removeLayer(layer); });
            window.routeLayers = [];
        };
        window.clearMarkers = function() {
            window.markers.forEach(function(marker) { map.removeLayer(marker); });
            window.markers = [];
        };
        window.addRoute = function(points, color) {
            if (points.length < 2) return;
            var latlngs = points.map(function(p) { return [p.lat, p.lng]; });
            var polyline = L.polyline(latlngs, { color: color, weight: 4, opacity: 0.8 }).addTo(map);
            window.routeLayers.push(polyline);
        };
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
        window.updateCurrentLocation = function(lat, lng) {
            if (window.currentLocationMarker) {
                window.currentLocationMarker.setLatLng([lat, lng]);
            } else {
                window.currentLocationMarker = L.circleMarker([lat, lng], {
                    radius: 10,
                    fillColor: '#00C896',
                    color: '#fff',
                    weight: 3,
                    opacity: 1,
                    fillOpacity: 0.9
                }).addTo(map);
            }
        };
        window.centerOn = function(lat, lng, zoom) {
            map.setView([lat, lng], zoom);
            window.updateCurrentLocation(lat, lng);
        };
    </script>
</body>
</html>");
        return sb.ToString();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.CurrentPoints) || 
            e.PropertyName == nameof(MapViewModel.CurrentLocation) ||
            e.PropertyName == nameof(MapViewModel.CurrentZoom) ||
            e.PropertyName == nameof(MapViewModel.IsAutoFollow))
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100);
                UpdateMapWithRoute();
            });
        }
    }

    private void UpdateMapWithRoute()
    {
        if (!_isMapInitialized) return;

        try
        {
            var js = new StringBuilder();
            js.Append("clearRoute();");
            js.Append("clearMarkers();");
            
            if (_viewModel.CurrentPoints.Count > 0)
            {
                string currentActivity = _viewModel.CurrentPoints[0].ActivityAtPoint;
                var currentSegment = new List<(double lat, double lng)>();
                
                for (int i = 0; i < _viewModel.CurrentPoints.Count; i++)
                {
                    var point = _viewModel.CurrentPoints[i];
                    
                    if (point.ActivityAtPoint != currentActivity && currentSegment.Count > 1)
                    {
                        var color = GetActivityColorHex(currentActivity);
                        js.Append($"addRoute({SerializePoints(currentSegment)}, '{color}');");
                        currentSegment.Clear();
                        currentActivity = point.ActivityAtPoint;
                    }
                    
                    currentSegment.Add((point.Latitude, point.Longitude));
                }
                
                if (currentSegment.Count > 1)
                {
                    var color = GetActivityColorHex(currentActivity);
                    js.Append($"addRoute({SerializePoints(currentSegment)}, '{color}');");
                }
                
                var start = _viewModel.CurrentPoints[0];
                js.Append($"addMarker({start.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {start.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, '#00C896');");
            }
            
            // Always update current location marker if we have location
            if (_viewModel.CurrentLocation != null)
            {
                js.Append($"updateCurrentLocation({_viewModel.CurrentLocation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {_viewModel.CurrentLocation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)});");
                
                if (_viewModel.IsAutoFollow)
                {
                    js.Append($"centerOn({_viewModel.CurrentLocation.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {_viewModel.CurrentLocation.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {_viewModel.CurrentZoom});");
                }
            }
            
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
