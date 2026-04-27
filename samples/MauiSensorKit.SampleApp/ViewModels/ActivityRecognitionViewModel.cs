using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MauiSensorKit;
using System.Collections.ObjectModel;

namespace MauiSensorKit.SampleApp.ViewModels;

public partial class ActivityRecognitionViewModel : ObservableObject, IDisposable
{
    private readonly ISensorCollectionService _sensorService;
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
    private DateTime _lastStepTime;
    private long _stepCount;
    private Queue<double> _accelHistory = new(50);
    private Queue<double> _gyroHistory = new(50);
    
    // Environment detection
    private double _lastNoiseLevel;

    public ActivityRecognitionViewModel(ISensorCollectionService sensorService)
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
            
            // Start sensor service
            await _sensorService.StartAsync();
            
            // Subscribe to sensor readings
            _sensorService.ReadingRecorded += OnSensorReading;
            
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
        
        // Stop sensor service
        _sensorService.ReadingRecorded -= OnSensorReading;
        await _sensorService.StopAsync();
        
        // Clear subscriptions
        foreach (var sub in _subscriptions)
        {
            sub.Dispose();
        }
        _subscriptions.Clear();
        
        AddActivityEvent("Monitoring stopped", "🛑");
    }

    private void OnSensorReading(object? sender, SensorReading reading)
    {
        switch (reading.Type)
        {
            case SensorType.Accelerometer when reading is AccelerometerReading accel:
                AnalyzeMotion(accel);
                break;
            case SensorType.Gyroscope when reading is GyroscopeReading gyro:
                AnalyzeRotation(gyro);
                break;
            case SensorType.Barometer when reading is BarometerReading baro:
                AnalyzePressure(baro);
                break;
            case SensorType.Microphone when reading is MicrophoneReading mic:
                AnalyzeNoise(mic);
                break;
            case SensorType.StepCounter when reading is StepCounterReading step:
                UpdateStepCount(step);
                break;
        }
    }

    private void AnalyzeMotion(AccelerometerReading reading)
    {
        // Calculate magnitude (removing gravity)
        var magnitude = Math.Sqrt(reading.X * reading.X + reading.Y * reading.Y + reading.Z * reading.Z);
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

    private void AnalyzeRotation(GyroscopeReading reading)
    {
        var magnitude = Math.Sqrt(reading.X * reading.X + reading.Y * reading.Y + reading.Z * reading.Z);
        
        _gyroHistory.Enqueue(magnitude);
        if (_gyroHistory.Count > 50) _gyroHistory.Dequeue();
    }

    private void AnalyzePressure(BarometerReading reading)
    {
        // Pressure changes indicate elevation changes (stairs, elevator, hill)
        var pressure = reading.PressureHPa;
        // Implementation for elevation detection
    }

    private void AnalyzeNoise(MicrophoneReading reading)
    {
        _lastNoiseLevel = reading.AmplitudeDb;
        
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

    private void UpdateStepCount(StepCounterReading reading)
    {
        var steps = reading.TotalSteps;
        if (steps > _stepCount)
        {
            _stepCount = steps;
            _lastStepTime = DateTime.Now;
        }
    }

    // Track motion state for better activity detection
    private int _consecutiveMotionReadings = 0;
    private int _consecutiveStationaryReadings = 0;
    private DateTime _lastActivityChangeTime = DateTime.Now;
    
    private void DetectActivityFromMotion()
    {
        if (_accelHistory.Count < 10) return;
        
        // Use last 10 readings for faster response
        var recent = _accelHistory.ToArray().TakeLast(10).ToArray();
        var avgMagnitude = recent.Average();
        var variance = recent.Select(v => Math.Pow(v - avgMagnitude, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        
        // Analyze gyroscope for rotation patterns
        var gyroAvg = _gyroHistory.Count > 0 ? _gyroHistory.ToArray().TakeLast(10).Average() : 0;
        
        // Calculate step frequency - use step counter OR detect from accelerometer
        var timeSinceLastStep = DateTime.Now - _lastStepTime;
        var hasStepCounter = timeSinceLastStep.TotalSeconds < 3;
        
        // Detect steps from accelerometer pattern (walking creates oscillating pattern)
        var accelRange = recent.Max() - recent.Min();
        var detectStepsFromAccel = accelRange > 1.5 && stdDev > 0.3;
        var isStepping = hasStepCounter || detectStepsFromAccel;
        
        // Activity detection logic
        if (CrashDetected) return;
        
        // Track motion state for hysteresis (prevents rapid switching)
        var isMoving = stdDev > 0.8 || isStepping || accelRange > 2.0;
        if (isMoving)
        {
            _consecutiveMotionReadings++;
            _consecutiveStationaryReadings = 0;
        }
        else
        {
            _consecutiveStationaryReadings++;
            _consecutiveMotionReadings = 0;
        }
        
        // Require 3 consecutive readings before changing to moving state
        // Require 5 consecutive readings before changing to stationary state
        var confirmedMoving = _consecutiveMotionReadings >= 3;
        var confirmedStationary = _consecutiveStationaryReadings >= 5;
        
        // Running detection (high variance, high acceleration range)
        if (confirmedMoving && (stdDev > 2.5 || accelRange > 6) && avgMagnitude > 11)
        {
            if (CurrentActivity != "Running")
            {
                UpdateActivity("Running", "🏃", "88%");
            }
            return;
        }
        
        // Walking detection (moderate variance, regular motion pattern)
        if (confirmedMoving && stdDev > 1.0 && stdDev < 3.5 && accelRange > 2.0 && accelRange < 6)
        {
            if (CurrentActivity != "Walking")
            {
                UpdateActivity("Walking", "🚶", "85%");
            }
            return;
        }
        
        // Vehicle detection (smooth motion with consistent low-frequency vibration)
        if (stdDev > 0.5 && stdDev < 2.5 && gyroAvg < 1.0 && !isStepping && confirmedMoving)
        {
            if (avgMagnitude > 9.8 && avgMagnitude < 11)
            {
                if (CurrentActivity != "In Car/Bus")
                {
                    UpdateActivity("In Car/Bus", "🚗", "75%");
                }
            }
            else if (avgMagnitude >= 11 && avgMagnitude < 12)
            {
                if (CurrentActivity != "On Train/Metro")
                {
                    UpdateActivity("On Train/Metro", "🚆", "78%");
                }
            }
            return;
        }
        
        // Stairs detection (irregular pattern with higher variance than walking)
        if (confirmedMoving && isStepping && stdDev > 1.5 && stdDev < 4 && accelRange > 3)
        {
            if (CurrentActivity != "On Stairs")
            {
                UpdateActivity("On Stairs", "🪜", "70%");
            }
            return;
        }
        
        // Crowd detection (irregular high variance + noise)
        if (stdDev > 3 && _lastNoiseLevel > 65 && !isStepping && confirmedMoving)
        {
            if (CurrentActivity != "In Crowd")
            {
                UpdateActivity("In Crowd", "👥", "72%");
            }
            return;
        }
        
        // Stationary detection (low variance, no steps) - requires more confirmations
        if (confirmedStationary && stdDev < 1.0 && !isStepping && accelRange < 1.5)
        {
            if (CurrentActivity != "Sitting/Stationary")
            {
                UpdateActivity("Sitting/Stationary", "🪑", "90%");
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
        try
        {
            // Fire and forget with ContinueWith to avoid blocking UI thread
            _ = StopMonitoringAsync().ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Error during dispose: {t.Exception.Message}");
                }
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
        catch
        {
            // Ignore exceptions during dispose
        }
    }
}

public class ActivityEvent
{
    public string Time { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
