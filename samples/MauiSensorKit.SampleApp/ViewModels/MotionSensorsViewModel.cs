using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSensorKit;
using Microcharts;
using SkiaSharp;

namespace MauiSensorKit.SampleApp.ViewModels;

public partial class MotionSensorsViewModel : ObservableObject, IDisposable
{
    private readonly ISensorCollectionService _sensorService;
    
    private readonly Queue<ChartEntry> _gyroX = new(50);
    private readonly Queue<ChartEntry> _gyroY = new(50);
    private readonly Queue<ChartEntry> _gyroZ = new(50);
    
    private readonly Queue<ChartEntry> _magX = new(50);
    private readonly Queue<ChartEntry> _magY = new(50);
    private readonly Queue<ChartEntry> _magZ = new(50);
    
    private readonly Queue<ChartEntry> _gravityX = new(50);
    private readonly Queue<ChartEntry> _gravityY = new(50);
    private readonly Queue<ChartEntry> _gravityZ = new(50);
    
    private readonly Queue<ChartEntry> _linearX = new(50);
    private readonly Queue<ChartEntry> _linearY = new(50);
    private readonly Queue<ChartEntry> _linearZ = new(50);
    
    private System.Threading.Timer? _updateTimer;
    private int _dataPointIndex;

    [ObservableProperty]
    private Chart? _gyroChart;
    
    [ObservableProperty]
    private Chart? _magnetometerChart;
    
    [ObservableProperty]
    private Chart? _gravityChart;
    
    [ObservableProperty]
    private Chart? _linearAccelChart;
    
    [ObservableProperty]
    private string _gyroValues = "X: --  Y: --  Z: --";
    
    [ObservableProperty]
    private string _magValues = "X: --  Y: --  Z: --";
    
    [ObservableProperty]
    private string _gravityValues = "X: --  Y: --  Z: --";
    
    [ObservableProperty]
    private string _linearValues = "X: --  Y: --  Z: --";
    
    [ObservableProperty]
    private bool _isRecording;
    
    [ObservableProperty]
    private string _statusMessage = "Tap Start to begin monitoring";

    public MotionSensorsViewModel(ISensorCollectionService sensorService)
    {
        _sensorService = sensorService;
        _sensorService.ReadingRecorded += OnSensorReading;
        
        // Initialize empty charts
        InitializeCharts();
        
        // Check if recording is already running (started from Dashboard)
        if (_sensorService.IsRunning)
        {
            IsRecording = true;
            StatusMessage = "Recording active - receiving sensor data...";
            _updateTimer = new System.Threading.Timer(_ => UpdateCharts(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }
    }

    private void InitializeCharts()
    {
        GyroChart = CreateEmptyChart("Gyroscope (rad/s)");
        MagnetometerChart = CreateEmptyChart("Magnetometer (μT)");
        GravityChart = CreateEmptyChart("Gravity (m/s²)");
        LinearAccelChart = CreateEmptyChart("Linear Acceleration (m/s²)");
    }

    private static LineChart CreateEmptyChart(string label)
    {
        return new LineChart
        {
            Entries = new[] { new ChartEntry(0) { Color = SKColors.Transparent } },
            LineMode = LineMode.Straight,
            LineSize = 2,
            PointMode = PointMode.None,
            LabelTextSize = 16,
            BackgroundColor = SKColors.Transparent,
            LabelColor = SKColors.Gray,
            MinValue = -10,
            MaxValue = 10
        };
    }

    private void OnSensorReading(object? sender, SensorReading reading)
    {
        try
        {
            switch (reading)
            {
                case GyroscopeReading gyro:
                    AddValue(_gyroX, gyro.X);
                    AddValue(_gyroY, gyro.Y);
                    AddValue(_gyroZ, gyro.Z);
                    MainThread.BeginInvokeOnMainThread(() => GyroValues = $"X: {gyro.X:F2}  Y: {gyro.Y:F2}  Z: {gyro.Z:F2}");
                    break;
                    
                case MagnetometerReading mag:
                    AddValue(_magX, mag.X);
                    AddValue(_magY, mag.Y);
                    AddValue(_magZ, mag.Z);
                    MainThread.BeginInvokeOnMainThread(() => MagValues = $"X: {mag.X:F1}  Y: {mag.Y:F1}  Z: {mag.Z:F1}");
                    break;
                    
                case GravitySensorReading grav:
                    AddValue(_gravityX, grav.X);
                    AddValue(_gravityY, grav.Y);
                    AddValue(_gravityZ, grav.Z);
                    MainThread.BeginInvokeOnMainThread(() => GravityValues = $"X: {grav.X:F2}  Y: {grav.Y:F2}  Z: {grav.Z:F2}");
                    break;
                    
                case LinearAccelerationReading lin:
                    AddValue(_linearX, lin.X);
                    AddValue(_linearY, lin.Y);
                    AddValue(_linearZ, lin.Z);
                    MainThread.BeginInvokeOnMainThread(() => LinearValues = $"X: {lin.X:F2}  Y: {lin.Y:F2}  Z: {lin.Z:F2}");
                    break;
            }
        }
        catch (Exception ex)
        {
            // Silently ignore errors in sensor reading handler
            System.Diagnostics.Debug.WriteLine($"Error in sensor reading handler: {ex.Message}");
        }
    }

    private void AddValue(Queue<ChartEntry> queue, double value)
    {
        _dataPointIndex++;
        var color = queue.Count % 3 == 0 ? SKColors.Red : (queue.Count % 3 == 1 ? SKColors.Green : SKColors.Blue);
        queue.Enqueue(new ChartEntry((float)value)
        {
            Color = color,
            Label = queue.Count % 10 == 0 ? $"{_dataPointIndex}" : ""
        });
        
        if (queue.Count > 50)
            queue.Dequeue();
    }

    [RelayCommand]
    private async Task StartRecordingAsync()
    {
        try
        {
            await _sensorService.StartAsync();
            IsRecording = true;
            StatusMessage = "Recording motion sensors...";
            
            _updateTimer = new System.Threading.Timer(_ => UpdateCharts(), null, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        await _sensorService.StopAsync();
        IsRecording = false;
        StatusMessage = "Recording stopped";
        _updateTimer?.Dispose();
        _updateTimer = null;
    }

    private void UpdateCharts()
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    GyroChart = CreateMultiSeriesChart(_gyroX, _gyroY, _gyroZ, "Gyroscope");
                    MagnetometerChart = CreateMultiSeriesChart(_magX, _magY, _magZ, "Magnetometer");
                    GravityChart = CreateMultiSeriesChart(_gravityX, _gravityY, _gravityZ, "Gravity");
                    LinearAccelChart = CreateMultiSeriesChart(_linearX, _linearY, _linearZ, "Linear Accel");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating charts: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error in UpdateCharts: {ex.Message}");
        }
    }

    private static LineChart CreateMultiSeriesChart(Queue<ChartEntry> x, Queue<ChartEntry> y, Queue<ChartEntry> z, string label)
    {
        try
        {
            var allEntries = new List<ChartEntry>();
            
            // Add X entries (Red)
            if (x.Count > 0)
            {
                allEntries.AddRange(x.Select((e, i) => new ChartEntry(e.Value) 
                { 
                    Color = SKColors.Red,
                    Label = i % 10 == 0 ? e.Label : ""
                }));
            }
            
            // Add Y entries (Green) - offset to separate lines
            if (y.Count > 0)
            {
                allEntries.AddRange(y.Select((e, i) => new ChartEntry(e.Value + 0.1f) 
                { 
                    Color = SKColors.Green,
                    Label = ""
                }));
            }
            
            // Add Z entries (Blue) - offset more
            if (z.Count > 0)
            {
                allEntries.AddRange(z.Select((e, i) => new ChartEntry(e.Value + 0.2f) 
                { 
                    Color = SKColors.Blue,
                    Label = ""
                }));
            }

            if (allEntries.Count == 0)
                allEntries.Add(new ChartEntry(0) { Color = SKColors.Transparent });

            return new LineChart
                {
                    Entries = allEntries,
                    LineMode = LineMode.Straight,
                    LineSize = 2,
                    PointMode = PointMode.Circle,
                    PointSize = 4,
                    LabelTextSize = 14,
                    BackgroundColor = SKColors.Transparent,
                    LabelColor = SKColors.Gray,
                    ValueLabelOrientation = Orientation.Horizontal
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating chart: {ex.Message}");
                // Return empty chart on error
                return new LineChart
                {
                    Entries = new[] { new ChartEntry(0) { Color = SKColors.Transparent } },
                    LineMode = LineMode.Straight,
                    LineSize = 2,
                    PointMode = PointMode.None,
                    LabelTextSize = 14,
                    BackgroundColor = SKColors.Transparent,
                    LabelColor = SKColors.Gray
                };
            }
        }

    public void Dispose()
    {
        _sensorService.ReadingRecorded -= OnSensorReading;
        _updateTimer?.Dispose();
    }
}
