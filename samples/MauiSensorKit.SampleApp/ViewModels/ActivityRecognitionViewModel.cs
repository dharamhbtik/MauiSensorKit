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
    
    // Motion analysis - larger buffers for better pattern recognition
    private DateTime _lastStepTime;
    private long _stepCount;
    private Queue<double> _accelHistory = new(100);
    private Queue<double> _gyroHistory = new(100);
    private Queue<DateTime> _accelTimestamps = new(100); // Track when each sample was taken
    
    // Environment detection
    private double _lastNoiseLevel;
    
    // Phone orientation for standing/sitting detection
    private double _lastTiltAngle;
    private bool _isPhoneVertical;
    
    // Barometer tracking for lift/escalator detection
    private double? _lastPressure;
    private DateTime _lastPressureChangeTime = DateTime.MinValue;
    private double _pressureChangeAccumulator;
    
    // State machine for robust activity tracking
    private string _confirmedActivity = "Unknown";
    private DateTime _activityStartTime = DateTime.Now;
    private int _consecutiveActivityReadings = 0;
    private const int RequiredConfirmations = 15; // ~1.5 seconds at 10Hz sampling
    private const int MinActivityDurationMs = 2000; // Minimum 2 seconds in activity before switching
    
    // Pattern analysis
    private List<double> _stepIntervals = new();
    private DateTime _lastPeakTime;
    private bool _wasAboveThreshold;

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
            case SensorType.GravitySensor when reading is GravitySensorReading gravity:
                // Use gravity sensor for better orientation detection if available
                CalculatePhoneOrientationFromGravity(gravity);
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
        
        // Detect crash/impact (very high acceleration spike - requires >5g sustained or >8g instant)
        // Real impacts are typically 50+ m/s² (5g+). A shake produces lower peaks.
        if (magnitude > 78) // ~8g instant spike indicates real impact
        {
            TriggerCrashDetection("High-G Impact Detected", magnitude);
            return;
        }
        
        // Also check for sustained high acceleration (multiple consecutive high readings)
        var recentHighReadings = _accelHistory.Count(a => a > 49); // >5g
        if (recentHighReadings >= 3 && magnitude > 49)
        {
            TriggerCrashDetection("Sustained Impact Detected", magnitude);
            return;
        }
        
        // Track phone orientation for standing/sitting detection
        CalculatePhoneOrientation(reading);
        
        // Store timestamp for pattern analysis
        _accelTimestamps.Enqueue(DateTime.Now);
        if (_accelTimestamps.Count > 100) _accelTimestamps.Dequeue();
        
        // Analyze patterns for activity detection
        DetectActivityFromMotionRobust();
    }

    private void TriggerCrashDetection(string type, double magnitude)
    {
        CrashDetected = true;
        CurrentActivity = $"{type}!";
        ActivityIcon = "💥";
        Confidence = "98%";
        AddActivityEvent($"{type}: {magnitude/9.8:F1}g", "💥");
        
        // Reset after 5 seconds - impacts are rare events
        Task.Run(async () =>
        {
            await Task.Delay(5000);
            CrashDetected = false;
        });
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
        var now = DateTime.Now;
        
        if (_lastPressure.HasValue)
        {
            var pressureChange = pressure - _lastPressure.Value;
            var timeSinceLastChange = now - _lastPressureChangeTime;
            
            // Track pressure changes for lift/escalator detection
            if (Math.Abs(pressureChange) > 0.1) // Significant pressure change
            {
                _pressureChangeAccumulator += pressureChange;
                _lastPressureChangeTime = now;
            }
            else if (timeSinceLastChange.TotalSeconds > 5)
            {
                // Reset accumulator if no change for 5 seconds
                _pressureChangeAccumulator = 0;
            }
        }
        
        _lastPressure = pressure;
    }
    
    /// <summary>
    /// Calculates phone tilt angle from accelerometer to determine if user is standing or sitting.
    /// </summary>
    private void CalculatePhoneOrientation(AccelerometerReading reading)
    {
        // Calculate tilt angle from vertical (gravity direction)
        // Z axis aligned with gravity when phone is flat on table
        // When standing holding phone, X or Y will have more gravity component
        var magnitude = Math.Sqrt(reading.X * reading.X + reading.Y * reading.Y + reading.Z * reading.Z);
        if (magnitude < 0.1) return; // Avoid division by zero
        
        // Calculate angle from vertical (Z axis)
        // cos(angle) = Z / magnitude, so angle = acos(Z / magnitude)
        var cosAngle = reading.Z / magnitude;
        cosAngle = Math.Clamp(cosAngle, -1.0, 1.0); // Ensure valid range
        var angleRadians = Math.Acos(cosAngle);
        var angleDegrees = angleRadians * 180.0 / Math.PI;
        
        _lastTiltAngle = angleDegrees;
        
        // Phone is considered vertical (user holding it while standing) if tilt > 45 degrees
        // Phone is considered horizontal (on lap/table while sitting) if tilt < 30 degrees
        _isPhoneVertical = angleDegrees > 45;
    }
    
    /// <summary>
    /// Calculates phone orientation from gravity sensor (more accurate than accelerometer).
    /// </summary>
    private void CalculatePhoneOrientationFromGravity(GravitySensorReading reading)
    {
        var magnitude = Math.Sqrt(reading.X * reading.X + reading.Y * reading.Y + reading.Z * reading.Z);
        if (magnitude < 0.1) return;
        
        var cosAngle = reading.Z / magnitude;
        cosAngle = Math.Clamp(cosAngle, -1.0, 1.0);
        var angleDegrees = Math.Acos(cosAngle) * 180.0 / Math.PI;
        
        _lastTiltAngle = angleDegrees;
        _isPhoneVertical = angleDegrees > 45;
    }

    private void AnalyzeNoise(MicrophoneReading reading)
    {
        _lastNoiseLevel = reading.AmplitudeDb;
        
        // Detect high noise environment (>80dB)
        if (_lastNoiseLevel > 80)
        {
            EnvironmentStatus = "High Noise Area";
            if (_confirmedActivity != "In Crowd" && _confirmedActivity != "High Noise Area")
            {
                UpdateActivityUI("High Noise Area");
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

    /// <summary>
    /// Robust activity detection using advanced pattern analysis.
    /// Distinguishes between rhythmic activities (walking, running) vs random motion (shake).
    /// Uses state machine with minimum durations for stable activity reporting.
    /// </summary>
    private void DetectActivityFromMotionRobust()
    {
        // Need sufficient data for pattern analysis
        if (_accelHistory.Count < 30) return;
        
        var now = DateTime.Now;
        var accelArray = _accelHistory.ToArray();
        var timestamps = _accelTimestamps.ToArray();
        
        // Use last 50 readings (~5 seconds at 10Hz) for pattern analysis
        var recent = accelArray.TakeLast(50).ToArray();
        var recentTimestamps = timestamps.TakeLast(50).ToArray();
        
        if (recent.Length < 30) return;
        
        // Basic statistics
        var avgMagnitude = recent.Average();
        var variance = recent.Select(v => Math.Pow(v - avgMagnitude, 2)).Average();
        var stdDev = Math.Sqrt(variance);
        var accelRange = recent.Max() - recent.Min();
        
        // Gyroscope analysis (for vehicle vs pedestrian detection)
        var gyroArray = _gyroHistory.ToArray();
        var recentGyro = gyroArray.TakeLast(30).ToArray();
        var gyroAvg = recentGyro.Length > 0 ? recentGyro.Average() : 0;
        var gyroStdDev = recentGyro.Length > 0 ? Math.Sqrt(recentGyro.Select(v => Math.Pow(v - gyroAvg, 2)).Average()) : 0;
        
        // Detect rhythmic patterns using zero-crossing analysis
        // Walking/running have consistent ~1-2Hz oscillations (gravity + vertical motion)
        var (isRhythmic, dominantFreq, rhythmConfidence) = AnalyzeRhythmicPattern(recent, recentTimestamps);
        
        // Step detection from hardware step counter
        var timeSinceLastStep = now - _lastStepTime;
        var hasRecentSteps = timeSinceLastStep.TotalSeconds < 3 && _stepCount > 0;
        
        // Activity classification with strict thresholds
        // Shake produces high stdDev but non-rhythmic; walking produces rhythmic medium stdDev
        var candidateActivity = ClassifyActivity(
            stdDev, 
            accelRange, 
            avgMagnitude,
            gyroAvg,
            gyroStdDev,
            isRhythmic,
            dominantFreq,
            rhythmConfidence,
            hasRecentSteps);
        
        // State machine - require confirmations and minimum duration
        UpdateActivityStateMachine(candidateActivity, now);
    }
    
    /// <summary>
    /// Analyzes accelerometer data for rhythmic patterns characteristic of walking/running.
    /// Returns: (isRhythmic, dominantFrequency, confidence)
    /// </summary>
    private (bool isRhythmic, double freq, double confidence) AnalyzeRhythmicPattern(double[] data, DateTime[] timestamps)
    {
        if (data.Length < 20) return (false, 0, 0);
        
        // Find peaks in the data (zero crossings from below to above mean)
        var mean = data.Average();
        var peaks = new List<double>(); // Time intervals between peaks
        double? lastPeakTime = null;
        
        for (int i = 1; i < data.Length - 1; i++)
        {
            // Peak is where previous point is lower and next point is lower
            if (data[i] > data[i-1] && data[i] > data[i+1] && data[i] > mean + 0.5)
            {
                if (lastPeakTime.HasValue)
                {
                    var interval = (timestamps[i] - DateTime.FromBinary((long)lastPeakTime.Value)).TotalSeconds;
                    if (interval > 0.3 && interval < 2.0) // Reasonable step interval
                    {
                        peaks.Add(interval);
                    }
                }
                lastPeakTime = timestamps[i].ToBinary();
            }
        }
        
        if (peaks.Count < 3) return (false, 0, 0);
        
        // Analyze consistency of intervals
        var avgInterval = peaks.Average();
        var intervalVariance = peaks.Select(p => Math.Pow(p - avgInterval, 2)).Average();
        var intervalStdDev = Math.Sqrt(intervalVariance);
        var consistency = 1.0 - (intervalStdDev / avgInterval); // 1 = perfect consistency
        
        // Frequency is steps per second
        var frequency = 1.0 / avgInterval;
        
        // Rhythmic if consistency > 0.6 and frequency in walking/running range (0.5 - 3 Hz)
        var isRhythmic = consistency > 0.6 && frequency >= 0.5 && frequency <= 3.0;
        
        return (isRhythmic, frequency, consistency);
    }
    
    /// <summary>
    /// Classifies motion based on statistical features and rhythmic analysis.
    /// </summary>
    private string ClassifyActivity(
        double stdDev, 
        double range, 
        double avgMagnitude,
        double gyroAvg,
        double gyroStdDev,
        bool isRhythmic,
        double dominantFreq,
        double rhythmConfidence,
        bool hasRecentSteps)
    {
        // PRIORITY 1: Lift/Escalator (altitude change with minimal motion)
        if (Math.Abs(_pressureChangeAccumulator) > 0.3 && stdDev < 0.5)
        {
            return "In Lift/Escalator";
        }
        
        // PRIORITY 2: Running (high energy + rhythmic + fast frequency > 2Hz)
        // Running: stdDev > 3, rhythmic, frequency > 2Hz
        if (isRhythmic && dominantFreq > 2.0 && stdDev > 3.0 && range > 8.0)
        {
            return "Running";
        }
        
        // PRIORITY 3: Walking (moderate energy + rhythmic + 1-2Hz frequency)
        // Walking: stdDev 1-4, rhythmic, frequency 1-2Hz
        if (isRhythmic && dominantFreq >= 1.0 && dominantFreq <= 2.0 && stdDev > 1.0 && stdDev < 5.0 && range > 2.0)
        {
            return "Walking";
        }
        
        // PRIORITY 4: Stairs (irregular high energy + stepping)
        // Stairs: higher variance than walking, irregular rhythm, still has steps
        if (hasRecentSteps && stdDev > 2.0 && stdDev < 6.0 && range > 5.0 && !isRhythmic)
        {
            return "On Stairs";
        }
        
        // PRIORITY 5: Vehicle detection
        // Vehicle: smooth consistent vibration, low gyro variance, no steps
        if (!hasRecentSteps && stdDev > 0.3 && stdDev < 1.5 && gyroStdDev < 0.5 && range < 3.0)
        {
            if (avgMagnitude > 9.8 && avgMagnitude < 11)
            {
                return "In Car/Bus";
            }
            else if (avgMagnitude >= 11 && avgMagnitude < 12)
            {
                return "On Train/Metro";
            }
        }
        
        // PRIORITY 6: Crowd/Busy environment
        // High noise + irregular motion + no clear steps
        if (_lastNoiseLevel > 70 && stdDev > 4.0 && !isRhythmic && !hasRecentSteps)
        {
            return "In Crowd";
        }
        
        // PRIORITY 7: Random motion (shake, fidgeting)
        // High stdDev but non-rhythmic - don't classify as walking
        if (stdDev > 2.0 && !isRhythmic && !hasRecentSteps)
        {
            return "Random Motion";
        }
        
        // PRIORITY 8: Stationary (Standing vs Sitting)
        // Very low motion
        if (stdDev < 0.15 && range < 0.4)
        {
            return _isPhoneVertical ? "Standing" : "Sitting";
        }
        
        // PRIORITY 9: Light movement (default for unclassified motion)
        if (stdDev > 0.15 && stdDev < 1.0)
        {
            return "Light Movement";
        }
        
        // Don't change if can't classify clearly
        return _confirmedActivity;
    }
    
    /// <summary>
    /// State machine that enforces minimum durations and confirmations before activity changes.
    /// Prevents rapid flip-flopping between activities.
    /// </summary>
    private void UpdateActivityStateMachine(string candidateActivity, DateTime now)
    {
        // If same activity, increment confirmations and reset timer
        if (candidateActivity == _confirmedActivity)
        {
            _consecutiveActivityReadings = Math.Min(_consecutiveActivityReadings + 1, RequiredConfirmations);
            return;
        }
        
        // Different activity - check if we've been in current activity long enough
        var timeInCurrentActivity = (now - _activityStartTime).TotalMilliseconds;
        
        // If candidate has been consistently detected for required confirmations
        // AND we've spent minimum time in current activity
        if (_consecutiveActivityReadings >= RequiredConfirmations && 
            timeInCurrentActivity >= MinActivityDurationMs)
        {
            // Transition to new activity
            var previousActivity = _confirmedActivity;
            _confirmedActivity = candidateActivity;
            _activityStartTime = now;
            _consecutiveActivityReadings = 0;
            
            // Update UI with appropriate icon and confidence
            UpdateActivityUI(candidateActivity);
        }
        else
        {
            // Reset counter for candidate activity - it's different from confirmed
            _consecutiveActivityReadings = 0;
        }
    }
    
    private void UpdateActivityUI(string activity)
    {
        var (icon, confidence) = activity switch
        {
            "Running" => ("🏃", "92%"),
            "Walking" => ("🚶", "88%"),
            "On Stairs" => ("🪜", "75%"),
            "In Car/Bus" => ("🚗", "80%"),
            "On Train/Metro" => ("🚆", "82%"),
            "In Lift/Escalator" => ("🛗", "85%"),
            "In Crowd" => ("👥", "70%"),
            "Standing" => ("🧍", "90%"),
            "Sitting" => ("🪑", "90%"),
            "Light Movement" => ("👋", "65%"),
            "Random Motion" => ("🔄", "60%"),
            _ => ("❓", "0%")
        };
        
        CurrentActivity = activity;
        ActivityIcon = icon;
        Confidence = confidence;
        AddActivityEvent($"Activity: {activity}", icon);
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
