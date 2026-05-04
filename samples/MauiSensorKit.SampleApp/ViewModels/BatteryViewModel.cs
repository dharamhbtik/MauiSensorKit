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
    private string _voltageText = "--";
    
    [ObservableProperty]
    private string _currentText = "--";
    
    [ObservableProperty]
    private string _temperatureText = "--";
    
    [ObservableProperty]
    private string _technologyText = "Unknown";
    
    [ObservableProperty]
    private string _healthText = "Unknown";
    
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
                UpdateBatteryDetails(currentBattery);
                
                // Add current snapshot to history for immediate display
                await _batteryHistoryService.RecordSnapshotAsync(currentBattery);
            }
            
            // Load history
            await RefreshData();
        }
        catch (Exception ex)
        {
            // Log error but don't crash
            EstimatedTimeString = "Battery info unavailable";
            System.Diagnostics.Debug.WriteLine($"BatteryViewModel init error: {ex.Message}");
        }
    }
    
    private async Task<BatterySnapshot?> GetCurrentBatteryFromDeviceAsync()
    {
        try
        {
            var level = Microsoft.Maui.Devices.Battery.ChargeLevel;
            var state = Microsoft.Maui.Devices.Battery.State;
            var powerSource = Microsoft.Maui.Devices.Battery.PowerSource;
            
            var snapshot = new BatterySnapshot
            {
                Timestamp = DateTimeOffset.Now,
                ChargeLevel = level,
                State = (BatteryState)state,
                PowerSource = (BatteryPowerSource)powerSource,
                VoltageVolts = null,
                CurrentMilliAmps = null,
                TemperatureCelsius = null,
                EstimatedRemainingMinutes = null,
                Technology = "Unknown",
                Health = BatteryHealth.Unknown,
                SessionId = _currentSessionId
            };

#if ANDROID
            // Get Android-specific battery information directly
            FillAndroidBatteryInfo(snapshot);
#endif

            return snapshot;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting battery: {ex.Message}");
            return null;
        }
    }

#if ANDROID
    private void FillAndroidBatteryInfo(BatterySnapshot snapshot)
    {
        try
        {
            var context = Android.App.Application.Context;
            if (context == null) return;

            var intentFilter = new Android.Content.IntentFilter(Android.Content.Intent.ActionBatteryChanged);
            var batteryStatus = context.RegisterReceiver(null, intentFilter);

            if (batteryStatus != null)
            {
                // Voltage in millivolts, convert to volts
                int voltageMv = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraVoltage, -1);
                if (voltageMv > 0)
                {
                    snapshot.VoltageVolts = voltageMv / 1000.0;
                }

                // Current in microamperes is usually queried via BatteryManager property rather than intent extra

                // Temperature in tenths of degree Celsius
                int tempTenths = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraTemperature, -1);
                if (tempTenths > 0)
                {
                    snapshot.TemperatureCelsius = tempTenths / 10.0;
                }

                // Technology (Li-ion, Li-poly, etc.)
                string? technology = batteryStatus.GetStringExtra(Android.OS.BatteryManager.ExtraTechnology);
                snapshot.Technology = technology ?? "Unknown";

                // Health
                int health = batteryStatus.GetIntExtra(Android.OS.BatteryManager.ExtraHealth, -1);
                snapshot.Health = ConvertAndroidHealth(health);

                // Battery Capacity and Current from BatteryManager
                var batteryManager = context.GetSystemService(Android.Content.Context.BatteryService) as Android.OS.BatteryManager;
                if (batteryManager != null)
                {
                    long remainingEnergy = batteryManager.GetLongProperty((int)Android.OS.BatteryProperty.ChargeCounter);
                    if (remainingEnergy > 0)
                    {
                        snapshot.CapacityRemainingMWh = (int)(remainingEnergy / 1000);
                    }

                    long remainingCapacity = batteryManager.GetLongProperty((int)Android.OS.BatteryProperty.Capacity);
                    if (remainingCapacity > 0)
                    {
                        snapshot.BatteryCapacityPercent = remainingCapacity / 100.0;
                    }
                    
                    long currentUa = batteryManager.GetLongProperty((int)Android.OS.BatteryProperty.CurrentNow);
                    if (currentUa != 0 && currentUa != long.MinValue)
                    {
                        snapshot.CurrentMilliAmps = currentUa / 1000.0;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Android battery: {snapshot.VoltageVolts}V, {snapshot.CurrentMilliAmps}mA, {snapshot.TemperatureCelsius}C, {snapshot.Technology}, {snapshot.Health}");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting Android battery info: {ex.Message}");
        }
    }

    private static BatteryHealth ConvertAndroidHealth(int health)
    {
        var androidHealth = (Android.OS.BatteryHealth)health;
        return androidHealth switch
        {
            Android.OS.BatteryHealth.Good => BatteryHealth.Good,
            Android.OS.BatteryHealth.Cold => BatteryHealth.Cold,
            Android.OS.BatteryHealth.Dead => BatteryHealth.Dead,
            Android.OS.BatteryHealth.Overheat => BatteryHealth.Overheat,
            Android.OS.BatteryHealth.OverVoltage => BatteryHealth.OverVoltage,
            Android.OS.BatteryHealth.UnspecifiedFailure => BatteryHealth.UnspecifiedFailure,
            _ => BatteryHealth.Unknown
        };
    }
#endif
    
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
                Technology = battery.Technology,
                Health = battery.Health,
                CapacityRemainingMWh = battery.CapacityRemainingMWh,
                BatteryCapacityPercent = battery.BatteryCapacityPercent,
                SessionId = _currentSessionId
            };
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentSnapshot = snapshot;
                UpdateHeroCard(snapshot);
                UpdateBatteryDetails(snapshot);
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
    
    private void UpdateBatteryDetails(BatterySnapshot snapshot)
    {
        // Update voltage display
        VoltageText = snapshot.VoltageVolts.HasValue 
            ? $"{snapshot.VoltageVolts.Value:F2}V" 
            : "--";
        
        // Update current display
        CurrentText = snapshot.CurrentMilliAmps.HasValue 
            ? $"{snapshot.CurrentMilliAmps.Value:F0}mA" 
            : "--";
        
        // Update temperature display
        TemperatureText = snapshot.TemperatureCelsius.HasValue 
            ? $"{snapshot.TemperatureCelsius.Value:F1}°C" 
            : "--";
        
        // Update technology
        TechnologyText = !string.IsNullOrEmpty(snapshot.Technology) 
            ? snapshot.Technology 
            : "Unknown";
        
        // Update health status
        HealthText = snapshot.Health switch
        {
            BatteryHealth.Good => "Good",
            BatteryHealth.Cold => "Cold",
            BatteryHealth.Dead => "Dead",
            BatteryHealth.Overheat => "Overheat",
            BatteryHealth.OverVoltage => "Over Voltage",
            BatteryHealth.UnspecifiedFailure => "Failure",
            BatteryHealth.GoodButFailure => "Good (Fail)",
            _ => "Unknown"
        };
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
        
        // Update metrics with fallback values
        if (Analytics != null)
        {
            DrainRateText = Analytics.AverageDischargeRatePerHour > 0 
                ? $"{Analytics.AverageDischargeRatePerHour:F1}% / hr" 
                : "Collecting...";
            
            var hours = Analytics.EstimatedFullDrainMinutes / 60 ?? 0;
            var mins = (Analytics.EstimatedFullDrainMinutes ?? 0) % 60;
            TimeRemainingText = Analytics.EstimatedFullDrainMinutes > 0 
                ? $"{hours:F0}h {mins:F0}m" 
                : "Calculating...";
            
            PeakTempText = Analytics.PeakTemperatureCelsius.HasValue 
                ? $"{Analytics.PeakTemperatureCelsius.Value:F0}°C" 
                : "--";
            ChargeCyclesText = Analytics.ChargingCyclesInSession > 0 
                ? Analytics.ChargingCyclesInSession.ToString() 
                : "0";
        }
        else
        {
            DrainRateText = "Collecting...";
            TimeRemainingText = "Calculating...";
            PeakTempText = "--";
            ChargeCyclesText = "0";
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
