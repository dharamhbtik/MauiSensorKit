namespace MauiSensorKit.SampleApp.Views;

using Mapsui;
using Mapsui.Layers;
using Mapsui.Nts;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using MauiSensorKit.SampleApp.ViewModels;
using NetTopologySuite.Geometries;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private Map _map;
    private MemoryLayer _routeLayer;

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
        
        // Create OpenStreetMap with free tiles when page appears
        InitializeMap();
        
        // Initial setup
        if (_viewModel.CurrentPoints.Count > 0)
        {
            UpdateRouteOnMap();
        }
    }
    
    private void InitializeMap()
    {
        if (_map != null) return; // Already initialized
        
        try
        {
            _map = new Map();
            _map.Layers.Add(OpenStreetMap.CreateTileLayer());
            
            // Initialize route layer
            _routeLayer = new MemoryLayer();
            _map.Layers.Add(_routeLayer);
            
            if (SensorMap != null)
            {
                SensorMap.Map = _map;
                
                // Start at user location or default
                var startMerc = SphericalMercator.FromLonLat(0, 0);
                _map.Home = n => n.CenterOnAndZoomTo(new MPoint(startMerc.x, startMerc.y), 3);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Map initialization error: {ex.Message}");
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MapViewModel.CurrentPoints))
        {
            MainThread.BeginInvokeOnMainThread(UpdateRouteOnMap);
        }
        else if (e.PropertyName == nameof(MapViewModel.CurrentLocation))
        {
            MainThread.BeginInvokeOnMainThread(MoveToCurrentLocation);
        }
    }

    private void UpdateRouteOnMap()
    {
        if (_viewModel.CurrentPoints.Count < 2) return;

        // Group points by activity and create line features
        var features = new List<IFeature>();
        var currentSegment = new List<Coordinate>();
        var currentActivity = _viewModel.CurrentPoints[0].ActivityAtPoint;

        for (int i = 0; i < _viewModel.CurrentPoints.Count; i++)
        {
            var point = _viewModel.CurrentPoints[i];
            
            if (point.ActivityAtPoint != currentActivity && currentSegment.Count > 1)
            {
                // Create line for previous segment
                var line = new LineString(currentSegment.ToArray());
                var feature = new GeometryFeature { Geometry = line };
                feature.Styles.Add(new VectorStyle
                {
                    Line = new Pen(GetActivityColor(currentActivity), 4)
                });
                features.Add(feature);
                
                currentSegment = new List<Coordinate>();
                currentActivity = point.ActivityAtPoint;
            }
            
            // Convert lat/lon to Spherical Mercator coordinates
            var mercator = SphericalMercator.FromLonLat(point.Longitude, point.Latitude);
            currentSegment.Add(new Coordinate(mercator.x, mercator.y));
        }

        // Add last segment
        if (currentSegment.Count > 1)
        {
            var line = new LineString(currentSegment.ToArray());
            var feature = new GeometryFeature { Geometry = line };
            feature.Styles.Add(new VectorStyle
            {
                Line = new Pen(GetActivityColor(currentActivity), 4)
            });
            features.Add(feature);
        }

        // Add start marker
        if (_viewModel.CurrentPoints.Count > 0)
        {
            var start = _viewModel.CurrentPoints[0];
            var startMercator = SphericalMercator.FromLonLat(start.Longitude, start.Latitude);
            var startFeature = new PointFeature(new MPoint(startMercator.x, startMercator.y));
            startFeature.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                Fill = new Brush(new Color(0x00, 0xC8, 0x96)),
                Outline = new Pen(new Color(0xFF, 0xFF, 0xFF), 2),
                SymbolScale = 0.5
            });
            features.Add(startFeature);
        }

        // Update layer
        _routeLayer.Features = features;
        _map.Refresh();
    }

    private void MoveToCurrentLocation()
    {
        if (_viewModel.CurrentLocation != null && _viewModel.IsAutoFollow)
        {
            var mercator = SphericalMercator.FromLonLat(_viewModel.CurrentLocation.Longitude, _viewModel.CurrentLocation.Latitude);
            SensorMap.Map.Navigator.CenterOnAndZoomTo(new MPoint(mercator.x, mercator.y), 15);
        }
    }

    private static Color GetActivityColor(string activity) => activity switch
    {
        "Walking" or "Walk" => new Color(0x00, 0xC8, 0x96),
        "Running" or "Run" => new Color(0xFF, 0x6B, 0x6B),
        "In Car/Bus" or "Driving" => new Color(0x6C, 0x63, 0xFF),
        "On Train/Metro" or "OnTrain" => new Color(0x00, 0xE5, 0xFF),
        "On Bus" => new Color(0xFF, 0x8C, 0x42),
        "On Stairs" or "Stairs" => new Color(0xFF, 0xB3, 0x47),
        "In Lift/Escalator" or "Elevator" => new Color(0xC0, 0x84, 0xFC),
        "Stationary" or "Standing" or "Sitting" => new Color(0x6B, 0x6B, 0x8A),
        _ => new Color(0x6C, 0x63, 0xFF)
    };

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
