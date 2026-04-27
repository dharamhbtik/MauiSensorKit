using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSensorKit;
using System.Collections.ObjectModel;
using SkiaSharp;
using Microcharts;

namespace MauiSensorKit.SampleApp.ViewModels;

/// <summary>
/// ViewModel for the comprehensive battery analytics page.
/// </summary>
public partial class BatteryViewModel : ObservableObject, IDisposable
{
    private readonly IBatteryHistoryService _batteryHistoryService;
    private readonly ISensorCollectionService _sensorService;
    private string _currentSessionId = string.Empty;
    private System.Threading.Timer? _refreshTimer;
    
    [ObservableProperty]
    private BatterySnapshot? _currentSnapshot;
    
    [ObservableProperty]
    private BatteryAnalytics? _analytics;
    
    [ObservableProperty]
    private ObservableCollection<BatterySnapshot> _historySnapshots = new();
    
    [ObservableProperty]
    private ObservableCollection<BatteryEvent> _events = new();
    
    [ObservableProperty]
    private string _selectedTimeRange = "Session"; // 1H, 6H, 24H, Session, All
    
    [ObservableProperty]
    private string _estimatedTimeString = "Calculating...";
    
    [ObservableProperty]
    private bool _isCharging;
    
    [ObservableProperty]
    private SKColor _heroCardColor = new SKColor(0x00, 0xC8, 0x96); // Green default
    
    [ObservableProperty]
    private Chart? _timelineChart;
    
    [ObservableProperty]
    private Chart? _drainRateChart;
    
    [ObservableProperty]
    private string _drainRateText = "--";
    
    [ObservableProperty]
    private string _timeRemainingText = "--";
    
    [ObservableProperty]
    private string _peakTempText = "--";
    
    [ObservableProperty]
    private string _chargeCyclesText = "--";
    
    [ObservableProperty]
    private bool _hasData;
    
    [ObservableProperty]
    private bool _isRecording;
    
    public BatteryViewModel(IBatteryHistoryService batteryHistoryService, ISensorCollectionService sensorService)
    {
        _batteryHistoryService = batteryHistoryService;
        _sensorService = sensorService;
        
        _sensorService.ReadingRecorded += OnSensorReading;
        _batteryHistoryService.BatteryEventDetected += OnBatteryEvent;
        
        // Initialize with current battery data immediately
        _ = InitializeAsync();
        
        // Refresh every 5 seconds
        _refreshTimer = new System.Threading.Timer(_ => _ = RefreshData(), null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
    }
    
    private async Task InitializeAsync()
    {
        try
        {
            // Start a session and load initial data
            _currentSessionId = _sensorService.CurrentSessionId ?? Guid.NewGuid().ToString("N");
            await _batteryHistoryService.StartSessionAsync(_currentSessionId);
            
            // Get current battery state from device
            var currentBattery = await GetCurrentBatteryFromDeviceAsync();
            if (currentBattery != null)
            {
                CurrentSnapshot = currentBattery;
                UpdateHeroCard(currentBattery);
            }
            
            // Load history
            await RefreshData();
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            EstimatedTimeString = "Battery info unavailable";
        }
    }
    
    private async Task<BatterySnapshot?> GetCurrentBatteryFromDeviceAsync()
    {
        try
        {
            var level = Microsoft.Maui.Devices.Battery.ChargeLevel;
            var state = Microsoft.Maui.Devices.Battery.State;
            var powerSource = Microsoft.Maui.Devices.Battery.PowerSource;
            
            return new BatterySnapshot
            {
                Timestamp = DateTimeOffset.Now,
                ChargeLevel = level,
                State = (BatteryState)state,
                PowerSource = (BatteryPowerSource)powerSource,
                VoltageVolts = null,
                CurrentMilliAmps = null,
                TemperatureCelsius = null,
                EstimatedRemainingMinutes = null,
                SessionId = _currentSessionId
            };
        }
        catch
        {
            return null;
        }
    }
    
    private void OnSensorReading(object? sender, SensorReading reading)
    {
        if (reading is BatteryReading battery)
        {
            var snapshot = new BatterySnapshot
            {
                Timestamp = battery.Timestamp,
                ChargeLevel = battery.ChargeLevel,
                State = battery.State,
                PowerSource = battery.PowerSource,
                VoltageVolts = battery.VoltageVolts,
                CurrentMilliAmps = battery.CurrentMilliAmps,
                TemperatureCelsius = battery.TemperatureCelsius,
                EstimatedRemainingMinutes = battery.EstimatedRemainingMinutes,
                SessionId = _currentSessionId
            };
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentSnapshot = snapshot;
                UpdateHeroCard(snapshot);
            });
        }
    }
    
    private void OnBatteryEvent(object? sender, BatteryEvent evt)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Events.Insert(0, evt);
            if (Events.Count > 50) Events.RemoveAt(Events.Count - 1);
        });
    }
    
    private void UpdateHeroCard(BatterySnapshot snapshot)
    {
        IsCharging = snapshot.State == BatteryState.Charging;
        
        // Determine hero card color based on charge level
        var percent = snapshot.ChargePercent;
        if (IsCharging && percent >= 99)
            HeroCardColor = new SKColor(0xC0, 0x84, 0xFC); // Purple (fully charged)
        else if (percent >= 50)
            HeroCardColor = new SKColor(0x00, 0xC8, 0x96); // Green
        else if (percent >= 20)
            HeroCardColor = new SKColor(0xFF, 0x8C, 0x42); // Amber
        else
            HeroCardColor = new SKColor(0xFF, 0x47, 0x57); // Red
        
        // Update estimated time
        if (IsCharging)
        {
            var remaining = 100 - percent;
            var minutes = remaining / Math.Max(1, Analytics?.AverageDischargeRatePerHour ?? 20) * 60;
            EstimatedTimeString = $"Est. full in {minutes:F0} min";
        }
        else
        {
            var hours = Analytics?.EstimatedFullDrainMinutes / 60 ?? 0;
            var mins = (Analytics?.EstimatedFullDrainMinutes ?? 0) % 60;
            EstimatedTimeString = $"Est. {hours:F0}h {mins:F0}m left";
        }
    }
    
    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        _currentSessionId = _sensorService.CurrentSessionId ?? "default";
        
        await _batteryHistoryService.StartSessionAsync(_currentSessionId);
        
        await RefreshData();
    }
    
    [RelayCommand]
    private async Task RefreshData()
    {
        if (string.IsNullOrEmpty(_currentSessionId)) return;
        
        var snapshots = await _batteryHistoryService.GetSessionHistoryAsync(_currentSessionId);
        var filtered = FilterByTimeRange(snapshots);
        
        HistorySnapshots.Clear();
        foreach (var s in filtered.OrderBy(s => s.Timestamp))
            HistorySnapshots.Add(s);
        
        Analytics = await _batteryHistoryService.ComputeAnalyticsAsync(_currentSessionId);
        
        // Update metrics
        if (Analytics != null)
        {
            DrainRateText = $"{Analytics.AverageDischargeRatePerHour:F1}% / hr";
            
            var hours = Analytics.EstimatedFullDrainMinutes / 60 ?? 0;
            var mins = (Analytics.EstimatedFullDrainMinutes ?? 0) % 60;
            TimeRemainingText = $"{hours:F0}h {mins:F0}m";
            
            PeakTempText = $"{Analytics.PeakTemperatureCelsius?.ToString("F0") ?? "--"}°C";
            ChargeCyclesText = Analytics.ChargingCyclesInSession.ToString();
        }
        
        HasData = HistorySnapshots.Count > 0;
        
        UpdateCharts();
    }
    
    private List<BatterySnapshot> FilterByTimeRange(List<BatterySnapshot> snapshots)
    {
        var now = DateTimeOffset.Now;
        return SelectedTimeRange switch
        {
            "1H" => snapshots.Where(s => s.Timestamp > now.AddHours(-1)).ToList(),
            "6H" => snapshots.Where(s => s.Timestamp > now.AddHours(-6)).ToList(),
            "24H" => snapshots.Where(s => s.Timestamp > now.AddHours(-24)).ToList(),
            _ => snapshots
        };
    }
    
    [RelayCommand]
    private void SelectTimeRange(string range)
    {
        SelectedTimeRange = range;
        _ = RefreshData();
    }
    
    private void UpdateCharts()
    {
        if (HistorySnapshots.Count < 2) return;
        
        // Timeline chart
        var entries = HistorySnapshots.Select(s => new ChartEntry((float)(s.ChargePercent))
        {
            Label = s.Timestamp.ToString("HH:mm"),
            ValueLabel = $"{s.ChargePercent:F0}%",
            Color = s.ChargePercent switch
            {
                >= 50 => new SKColor(0x00, 0xC8, 0x96),
                >= 20 => new SKColor(0xFF, 0x8C, 0x42),
                _ => new SKColor(0xFF, 0x47, 0x57)
            }
        }).ToArray();
        
        TimelineChart = new LineChart
        {
            Entries = entries,
            LineMode = LineMode.Spline,
            LineSize = 3,
            PointMode = PointMode.Circle,
            PointSize = 6,
            LabelTextSize = 24,
            BackgroundColor = SKColors.Transparent,
            LabelColor = new SKColor(0x6B, 0x6B, 0x8A),
            MinValue = 0,
            MaxValue = 100
        };
        
        // Drain rate chart (hourly buckets)
        var hourlyDrain = ComputeHourlyDrain();
        var drainEntries = hourlyDrain.Select((rate, i) => new ChartEntry((float)rate)
        {
            Label = $"{i:00}:00",
            Color = rate switch
            {
                < 3 => new SKColor(0x00, 0xC8, 0x96),  // Green - efficient
                < 6 => new SKColor(0xFF, 0x8C, 0x42),  // Amber - normal
                _ => new SKColor(0xFF, 0x47, 0x57)     // Red - heavy
            }
        }).ToArray();
        
        DrainRateChart = new BarChart
        {
            Entries = drainEntries,
            LabelTextSize = 24,
            BackgroundColor = SKColors.Transparent,
            LabelColor = new SKColor(0x6B, 0x6B, 0x8A)
        };
    }
    
    private List<double> ComputeHourlyDrain()
    {
        var hourly = new List<double>();
        var grouped = HistorySnapshots
            .GroupBy(s => s.Timestamp.Hour)
            .OrderBy(g => g.Key);
        
        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(s => s.Timestamp).ToList();
            if (ordered.Count >= 2)
            {
                var start = ordered.First().ChargeLevel;
                var end = ordered.Last().ChargeLevel;
                hourly.Add((start - end) * 100);
            }
            else
            {
                hourly.Add(0);
            }
        }
        
        return hourly.Count > 0 ? hourly : new List<double> { 0, 0, 0, 0, 0, 0 };
    }
    
    [RelayCommand]
    private async Task ExportBatteryDataAsync()
    {
        // Implementation for exporting battery data
        await Task.CompletedTask;
    }
    
    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _sensorService.ReadingRecorded -= OnSensorReading;
        _batteryHistoryService.BatteryEventDetected -= OnBatteryEvent;
    }
}
