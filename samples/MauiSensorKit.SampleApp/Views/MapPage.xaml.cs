namespace MauiSensorKit.SampleApp.Views;

using MauiSensorKit.SampleApp.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

public partial class MapPage : ContentPage
{
    private readonly MapViewModel _viewModel;
    private List<Polyline> _polylines = new();

    public MapPage(MapViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        // Subscribe to viewmodel changes
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
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
        // Clear existing polylines
        foreach (var polyline in _polylines)
        {
            SensorMap.MapElements.Remove(polyline);
        }
        _polylines.Clear();

        if (_viewModel.CurrentPoints.Count < 2) return;

        // Group points by activity and create polylines
        var segments = new List<(List<Location> locations, string activity)>();
        var currentSegment = new List<Location> { new Location(_viewModel.CurrentPoints[0].Latitude, _viewModel.CurrentPoints[0].Longitude) };
        var currentActivity = _viewModel.CurrentPoints[0].ActivityAtPoint;

        for (int i = 1; i < _viewModel.CurrentPoints.Count; i++)
        {
            var point = _viewModel.CurrentPoints[i];
            if (point.ActivityAtPoint != currentActivity)
            {
                segments.Add((currentSegment, currentActivity));
                currentSegment = new List<Location>();
                currentActivity = point.ActivityAtPoint;
            }
            currentSegment.Add(new Location(point.Latitude, point.Longitude));
        }
        segments.Add((currentSegment, currentActivity));

        // Create polylines for each segment
        foreach (var (locations, activity) in segments)
        {
            if (locations.Count < 2) continue;

            var polyline = new Polyline
            {
                StrokeColor = GetActivityColor(activity),
                StrokeWidth = 5
            };

            foreach (var loc in locations)
            {
                polyline.Geopath.Add(loc);
            }

            SensorMap.MapElements.Add(polyline);
            _polylines.Add(polyline);
        }
    }

    private void MoveToCurrentLocation()
    {
        if (_viewModel.CurrentLocation != null && _viewModel.IsAutoFollow)
        {
            SensorMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                _viewModel.CurrentLocation,
                Distance.FromMeters(500)));
        }
    }

    private static Color GetActivityColor(string activity) => activity switch
    {
        "Walking" or "Walk" => Color.FromArgb("#00C896"),
        "Running" or "Run" => Color.FromArgb("#FF6B6B"),
        "In Car/Bus" or "Driving" => Color.FromArgb("#6C63FF"),
        "On Train/Metro" or "OnTrain" => Color.FromArgb("#00E5FF"),
        "On Bus" => Color.FromArgb("#FF8C42"),
        "On Stairs" or "Stairs" => Color.FromArgb("#FFB347"),
        "In Lift/Escalator" or "Elevator" => Color.FromArgb("#C084FC"),
        "Stationary" or "Standing" or "Sitting" => Color.FromArgb("#6B6B8A"),
        _ => Color.FromArgb("#6C63FF")
    };

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
