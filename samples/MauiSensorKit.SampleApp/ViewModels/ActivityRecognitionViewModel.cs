using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSensorKit;
using MauiSensorKit.Abstractions;
using System.Collections.ObjectModel;

namespace MauiSensorKit.SampleApp.ViewModels;

public partial class ActivityRecognitionViewModel : ObservableObject, IDisposable
{
    private readonly ISensorService _sensorService;
    private readonly List<IDisposable> _subscriptions = new();
    
    [ObservableProperty]
    private string _currentActivity = "Unknown";
    
    [ObservableProperty]
    private string _activityIcon = "❓";
    
    [ObservableProperty]
    private string _confidence = "0%";
    
    [ObservableProperty]
    private bool _isMonitoring;
    
    [ObservableProperty]
    private string _statusMessage = "Tap Start to begin activity recognition";
    
    [ObservableProperty]
    private ObservableCollection<ActivityEvent> _activityHistory = new();
    
    [ObservableProperty]
    private string _environmentStatus = "Normal";
    
    [ObservableProperty]
    private bool _crashDetected;
    
    // Motion analysis
    private double _lastAccelMagnitude;
    private DateTime _lastStepTime;
    private int _stepCount;
    private Queue<double> _accelHistory = new(50);
    private Queue<double> _gyroHistory = new(50);
    
    // Environment detection
    private double _lastNoiseLevel;
    private double _avgNoiseLevel;

    public ActivityRecognitionViewModel(ISensorService sensorService)
    {
        _sensorService = sensorService;
    }

    [RelayCommand]
    private async Task StartMonitoringAsync()
    {
        try
        {
            IsMonitoring = true;
            StatusMessage = "Analyzing sensor data...";
            ActivityHistory.Clear();
            
            // Subscribe to accelerometer for motion analysis
            var accelCollector = _sensorService.GetCollector(SensorType.Accelerometer);
            if (accelCollector != null)
            {
                _subscriptions.Add(accelCollector.Data.Subscribe(data =>
                {
                    AnalyzeMotion(data);
                }));
                await accelCollector.StartAsync();
            }
            
            // Subscribe to gyroscope for rotation analysis
            var gyroCollector = _sensorService.GetCollector(SensorType.Gyroscope);
            if (gyroCollector != null)
            {
                _subscriptions.Add(gyroCollector.Data.Subscribe(data =>
                {
                    AnalyzeRotation(data);
                }));
                await gyroCollector.StartAsync();
            }
            
            // Subscribe to barometer for elevation/pressure changes (indicates stairs/elevator)
            var baroCollector = _sensorService.GetCollector(SensorType.Barometer);
            if (baroCollector != null)
            {
                _subscriptions.Add(baroCollector.Data.Subscribe(data =>
                {
                    AnalyzePressure(data);
                }));
                await baroCollector.StartAsync();
            }
            
            // Subscribe to microphone for noise level detection
            var micCollector = _sensorService.GetCollector(SensorType.Microphone);
            if (micCollector != null)
            {
                _subscriptions.Add(micCollector.Data.Subscribe(data =>
                {
                    AnalyzeNoise(data);
                }));
                await micCollector.StartAsync();
            }
            
            // Subscribe to step counter
            var stepCollector = _sensorService.GetCollector(SensorType.StepCounter);
            if (stepCollector != null)
            {
                _subscriptions.Add(stepCollector.Data.Subscribe(data =>
                {
                    UpdateStepCount(data);
                }));
                await stepCollector.StartAsync();
            }
            
            StatusMessage = "Monitoring active - analyzing patterns...";
            AddActivityEvent("Monitoring started", "🚀");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsMonitoring = false;
        }
    }

    [RelayCommand]
    private async Task StopMonitoringAsync()
    {
        IsMonitoring = false;
        StatusMessage = "Monitoring stopped";
        
        // Stop all collectors
        foreach (var sensorType in new[] { SensorType.Accelerometer, SensorType.Gyroscope, SensorType.Barometer, SensorType.Microphone, SensorType.StepCounter })
        {
            var collector = _sensorService.GetCollector(sensorType);
            if (collector != null)
            {
                await collector.StopAsync();
            }
        }
        
        // Clear subscriptions
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();
        
        AddActivityEvent("Monitoring stopped", "🛑");
    }

    private void AnalyzeMotion(SensorData data)
    {
        if (data.Values.Length < 3) return;
        
        var x = data.Values[0];
        var y = data.Values[1];
        var z = data.Values[2];
        
        // Calculate magnitude (removing gravity)
        var magnitude = Math.Sqrt(x * x + y * y + z * z);
        _accelHistory.Enqueue(magnitude);
        if (_accelHistory.Count > 50) _accelHistory.Dequeue();
        
        // Detect crash (high acceleration spike)
        if (magnitude > 25) // ~2.5g spike
        {
            CrashDetected = true;
            CurrentActivity = "CRASH DETECTED!";
            ActivityIcon = "💥";
            Confidence = "95%";
            AddActivityEvent("Crash/Impact detected!", "💥");
            
            // Reset after 3 seconds
            Task.Run(async () =>
            {
                await Task.Delay(3000);
                CrashDetected = false;
            });
            return;
        }
        
        // Analyze patterns for activity detection
        DetectActivityFromMotion();
    }

    private void AnalyzeRotation(SensorData data)
    {
        if (data.Values.Length < 3) return;
        
        var magnitude = Math.Sqrt(data.Values[0] * data.Values[0] + 
                                  data.Values[1] * data.Values[1] + 
                                  data.Values[2] * data.Values[2]);
        
        _gyroHistory.Enqueue(magnitude);
        if (_gyroHistory.Count > 50) _gyroHistory.Dequeue();
    }

    private void AnalyzePressure(SensorData data)
    {
        // Pressure changes indicate elevation changes (stairs, elevator, hill)
        if (data.Values.Length > 0)
        {
            var pressure = data.Values[0];
            // Implementation for elevation detection
        }
    }

    private void AnalyzeNoise(SensorData data)
    {
        if (data.Values.Length > 0)
        {
            _lastNoiseLevel = data.Values[0];
            
            // Detect high noise environment (>80dB)
            if (_lastNoiseLevel > 80)
            {
                EnvironmentStatus = "High Noise Area";
                if (CurrentActivity != "In Crowd" && CurrentActivity != "High Noise Area")
                {
                    UpdateActivity("High Noise Area", "🔊", "85%");
                }
            }
            else if (_lastNoiseLevel > 60)
            {
                EnvironmentStatus = "Moderate Noise";
            }
            else
            {
                EnvironmentStatus = "Quiet";
            }
        }
    }

    private void UpdateStepCount(SensorData data)
    {
        if (data.Values.Length > 0)
        {
            var steps = (int)data.Values[0];
            if (steps > _stepCount)
            {
                _stepCount = steps;
                _lastStepTime = DateTime.Now;
            }
        }
    }

    private void DetectActivityFromMotion()
    {
        if (_accelHistory.Count < 20) return;
        
        var recent = _accelHistory.ToArray().TakeLast(20).ToArray();
        var avgMagnitude = recent.Average();
        var variance = recent.Select(v => Math.Pow(v - avgMagnitude, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        
        // Analyze gyroscope for rotation patterns
        var gyroAvg = _gyroHistory.Count > 0 ? _gyroHistory.ToArray().TakeLast(20).Average() : 0;
        
        // Calculate step frequency
        var timeSinceLastStep = DateTime.Now - _lastStepTime;
        var isStepping = timeSinceLastStep.TotalSeconds < 2;
        
        // Activity detection logic
        if (CrashDetected) return;
        
        // Stationary detection (very low variance)
        if (stdDev < 0.5 && !isStepping)
        {
            if (CurrentActivity != "Sitting/Stationary")
            {
                UpdateActivity("Sitting/Stationary", "🪑", "90%");
            }
            return;
        }
        
        // Walking detection (regular steps, moderate variance)
        if (isStepping && stdDev > 0.5 && stdDev < 3 && avgMagnitude > 9.5 && avgMagnitude < 12)
        {
            if (CurrentActivity != "Walking")
            {
                UpdateActivity("Walking", "🚶", "85%");
            }
            return;
        }
        
        // Running detection (high variance, rapid steps)
        if (isStepping && stdDev > 3 && avgMagnitude > 12)
        {
            if (CurrentActivity != "Running")
            {
                UpdateActivity("Running", "🏃", "88%");
            }
            return;
        }
        
        // Vehicle detection (smooth motion with consistent vibration)
        if (stdDev > 1 && stdDev < 4 && gyroAvg < 0.5 && !isStepping)
        {
            // Distinguish between different vehicle types based on vibration patterns
            if (avgMagnitude > 10 && avgMagnitude < 11)
            {
                if (CurrentActivity != "In Car/Bus")
                {
                    UpdateActivity("In Car/Bus", "🚗", "75%");
                }
            }
            else if (avgMagnitude > 11)
            {
                if (CurrentActivity != "On Train/Metro")
                {
                    UpdateActivity("On Train/Metro", "🚆", "78%");
                }
            }
            return;
        }
        
        // Stairs detection (irregular pattern with elevation change)
        if (isStepping && stdDev > 2 && stdDev < 5 && _gyroHistory.Count > 0)
        {
            if (CurrentActivity != "On Stairs")
            {
                UpdateActivity("On Stairs", "🪜", "70%");
            }
            return;
        }
        
        // Crowd detection (irregular high variance + noise)
        if (stdDev > 4 && _lastNoiseLevel > 70 && !isStepping)
        {
            if (CurrentActivity != "In Crowd")
            {
                UpdateActivity("In Crowd", "👥", "72%");
            }
            return;
        }
    }

    private void UpdateActivity(string activity, string icon, string confidence)
    {
        if (CurrentActivity != activity)
        {
            CurrentActivity = activity;
            ActivityIcon = icon;
            Confidence = confidence;
            AddActivityEvent($"Detected: {activity}", icon);
        }
    }

    private void AddActivityEvent(string description, string icon)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            ActivityHistory.Insert(0, new ActivityEvent
            {
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Description = description,
                Icon = icon
            });
            
            // Keep only last 50 events
            while (ActivityHistory.Count > 50)
            {
                ActivityHistory.RemoveAt(ActivityHistory.Count - 1);
            }
        });
    }

    public void Dispose()
    {
        StopMonitoringAsync().Wait();
    }
}

public class ActivityEvent
{
    public string Time { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
