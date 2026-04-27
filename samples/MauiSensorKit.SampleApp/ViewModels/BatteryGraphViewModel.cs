using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSensorKit.SampleApp.Services;
using Microcharts;
using SkiaSharp;

namespace MauiSensorKit.SampleApp.ViewModels;

/// <summary>
/// ViewModel for the battery graph page.
/// </summary>
public partial class BatteryGraphViewModel : ObservableObject
{
    private readonly BatteryDataStore _batteryDataStore;
    private readonly ISensorCollectionService _sensorService;

    [ObservableProperty]
    private Chart? _batteryChart;

    [ObservableProperty]
    private string _currentPercentage = "--";

    [ObservableProperty]
    private string _currentState = "Unknown";

    [ObservableProperty]
    private string _powerSource = "Unknown";

    [ObservableProperty]
    private string _recordingDuration = "0:00";

    [ObservableProperty]
    private int _readingCount;

    [ObservableProperty]
    private bool _hasData;

    private System.Threading.Timer? _updateTimer;

    public BatteryGraphViewModel(BatteryDataStore batteryDataStore, ISensorCollectionService sensorService)
    {
        _batteryDataStore = batteryDataStore;
        _sensorService = sensorService;

        // Get current battery reading immediately (even if not recording)
        var currentReading = _batteryDataStore.GetCurrentReading();
        if (currentReading != null)
        {
            // Use MainThread for initial UI updates
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentPercentage = $"{currentReading.Percentage:F0}%";
                CurrentState = currentReading.State;
                PowerSource = currentReading.PowerSource;
                HasData = true;
            });
        }

        // Check if recording is already running (started from Dashboard)
        if (_sensorService.IsRunning)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentState = "Recording active - monitoring battery...";
            });
        }

        _updateTimer = new System.Threading.Timer(_ => UpdateChart(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    }

    [RelayCommand]
    private void RefreshChart()
    {
        UpdateChart();
    }

    [RelayCommand]
    private void ClearData()
    {
        _batteryDataStore.Clear();
        UpdateChart();
    }

    private void UpdateChart()
    {
        var readings = _batteryDataStore.GetReadings();
        var hasReadings = readings.Count > 0;

        // Always refresh current battery reading from device
        var currentReading = _batteryDataStore.GetCurrentReading();
        string percentage = "--";
        string state = CurrentState;
        string power = PowerSource;
        
        if (currentReading != null)
        {
            percentage = $"{currentReading.Percentage:F0}%";
            state = !_sensorService.IsRunning ? currentReading.State : CurrentState;
            power = currentReading.PowerSource;
        }

        string recordingDuration = RecordingDuration;
        if (_sensorService.IsRunning)
        {
            var duration = _batteryDataStore.GetSessionDuration();
            recordingDuration = $"{duration.Hours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
        }

        // Always create a valid chart to prevent crashes
        var entries = new List<ChartEntry>();

        // Prepare chart data
        Chart chart;
        if (readings.Count == 0)
        {
            // Empty chart with placeholder - but still show current battery level as single point
            if (currentReading != null)
            {
                entries.Add(new ChartEntry((float)currentReading.Percentage)
                {
                    Label = "Now",
                    ValueLabel = $"{currentReading.Percentage:F0}%",
                    Color = SKColors.Green
                });
            }
            else
            {
                entries.Add(new ChartEntry(0)
                {
                    Label = "",
                    Color = SKColors.Transparent
                });
            }
            chart = CreateLineChart(entries);
        }
        else
        {
            // Use latest from recorded data for text display
            var latest = readings.Last();
            percentage = $"{latest.Percentage:F0}%";
            state = latest.State;
            power = latest.PowerSource;

            // Create chart entries - show last 60 readings max for readability
            var displayReadings = readings.TakeLast(60).ToList();

            for (int i = 0; i < displayReadings.Count; i++)
            {
                var r = displayReadings[i];
                var color = r.State.Contains("Charging") || r.State.Contains("Full")
                    ? SKColors.Green
                    : SKColors.Red;

                entries.Add(new ChartEntry((float)r.Percentage)
                {
                    Label = i % 10 == 0 ? $"{i}" : "", // Show label every 10 points
                    ValueLabel = i == displayReadings.Count - 1 ? $"{r.Percentage:F0}%" : "",
                    Color = color
                });
            }
            chart = CreateLineChart(entries);
        }

        // Update all UI properties on main thread
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ReadingCount = readings.Count;
            HasData = hasReadings || currentReading != null;
            CurrentPercentage = percentage;
            CurrentState = state;
            PowerSource = power;
            RecordingDuration = recordingDuration;
            BatteryChart = chart;
        });
    }

    private static LineChart CreateLineChart(List<ChartEntry> entries)
    {
        return new LineChart
        {
            Entries = entries,
            LineMode = LineMode.Straight,
            LineSize = 3,
            PointMode = entries.Count > 1 ? PointMode.Circle : PointMode.None,
            PointSize = 8,
            LabelTextSize = 20,
            BackgroundColor = SKColors.Transparent,
            LabelColor = SKColors.Gray,
            ValueLabelOrientation = Orientation.Horizontal,
            MinValue = 0,
            MaxValue = 100
        };
    }

    public void Dispose()
    {
        _updateTimer?.Dispose();
    }
}
