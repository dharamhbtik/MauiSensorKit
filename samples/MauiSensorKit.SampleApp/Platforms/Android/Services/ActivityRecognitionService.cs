using Android.App;
using Android.Content;
using Android.Gms.Location;
using Android.OS;
using Android.Runtime;
using Java.Util.Concurrent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Activity = Android.App.Activity;

namespace MauiSensorKit.SampleApp.Platforms.Android.Services;

/// <summary>
/// Pro-level activity recognition using Google's Activity Recognition API.
/// This uses machine learning models trained by Google for high accuracy.
/// </summary>
public class ActivityRecognitionService : Java.Lang.Object, IActivityRecognitionCallback
{
    private ActivityRecognitionClient? _client;
    private ActivityRecognitionCallback? _callback;
    private IActivityRecognitionListener? _listener;
    private bool _isRunning;

    // GPS speed tracking for vehicle detection enhancement
    private double _currentSpeedMps;
    private DateTime _lastSpeedUpdate = DateTime.MinValue;

    // Activity confidence thresholds (pro-level tuning)
    private const int HIGH_CONFIDENCE = 75;
    private const int MEDIUM_CONFIDENCE = 50;
    private const int LOW_CONFIDENCE = 25;

    // GPS speed thresholds for vehicle detection (m/s)
    private const double WALKING_SPEED_MAX = 2.0;      // ~7 km/h
    private const double RUNNING_SPEED_MAX = 5.0;      // ~18 km/h
    private const double VEHICLE_SPEED_MIN = 3.0;    // ~11 km/h
    private const double HIGH_SPEED_MIN = 15.0;       // ~54 km/h (train/metro)

    public bool IsRunning => _isRunning;

    public void Initialize(Context context)
    {
        _client = ActivityRecognition.GetClient(context);
    }

    public async Task<bool> StartAsync(IActivityRecognitionListener listener)
    {
        if (_isRunning || _client == null) return false;

        try
        {
            _listener = listener;
            _callback = new ActivityRecognitionCallback(this);

            // Request activity updates every 2 seconds (pro-level frequency)
            var task = _client.RequestActivityUpdatesAsync(2000, _callback);
            await task;

            _isRunning = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ActivityRecognition start error: {ex.Message}");
            return false;
        }
    }

    public async Task StopAsync()
    {
        if (!_isRunning || _client == null) return;

        try
        {
            await _client.RemoveActivityUpdatesAsync(_callback);
            _isRunning = false;
            _listener = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ActivityRecognition stop error: {ex.Message}");
        }
    }

    /// <summary>
    /// Update current GPS speed for enhanced vehicle detection
    /// </summary>
    public void UpdateGpsSpeed(double speedMps)
    {
        _currentSpeedMps = speedMps;
        _lastSpeedUpdate = DateTime.Now;
    }

    /// <summary>
    /// Process detected activities from Google's API and apply pro-level logic
    /// </summary>
    internal void OnActivityResult(IList<DetectedActivity>? activities)
    {
        if (activities == null || activities.Count == 0 || _listener == null) return;

        // Get the most probable activity
        var detectedActivity = GetBestActivity(activities);
        if (detectedActivity == null) return;

        var activityType = ConvertActivityType(detectedActivity.Type);
        var confidence = detectedActivity.Confidence;

        // Apply pro-level logic with GPS speed validation
        var (finalActivity, finalConfidence) = ApplyProLevelLogic(activityType, confidence, activities);

        _listener.OnActivityDetected(finalActivity, finalConfidence);
    }

    /// <summary>
    /// Get the most confident activity from the list
    /// </summary>
    private DetectedActivity? GetBestActivity(IList<DetectedActivity> activities)
    {
        return activities.OrderByDescending(a => a.Confidence).FirstOrDefault();
    }

    /// <summary>
    /// Pro-level logic: Combine ML results with GPS speed and motion patterns
    /// </summary>
    private (string activity, int confidence) ApplyProLevelLogic(
        string baseActivity, int baseConfidence, IList<DetectedActivity> allActivities)
    {
        var speed = _currentSpeedMps;
        var speedValid = (DateTime.Now - _lastSpeedUpdate).TotalSeconds < 10; // Speed data within 10s

        // Get secondary activities for context
        var hasVehicle = allActivities.Any(a => a.Type == DetectedActivity.InVehicle && a.Confidence > 30);
        var hasBicycle = allActivities.Any(a => a.Type == DetectedActivity.OnBicycle && a.Confidence > 30);
        var hasWalking = allActivities.Any(a => a.Type == DetectedActivity.Walking && a.Confidence > 30);
        var hasRunning = allActivities.Any(a => a.Type == DetectedActivity.Running && a.Confidence > 30);
        var hasStill = allActivities.Any(a => a.Type == DetectedActivity.Still && a.Confidence > 30);

        // Rule 1: High speed detection (train/metro/bus on highway)
        if (speedValid && speed > HIGH_SPEED_MIN)
        {
            // At high speeds, it's definitely a vehicle
            return ("On Train/Metro", 95);
        }

        // Rule 2: Vehicle speed range
        if (speedValid && speed > VEHICLE_SPEED_MIN)
        {
            if (hasVehicle || baseActivity == "In Car/Bus")
            {
                return ("In Car/Bus", Math.Max(baseConfidence, 85));
            }
            // GPS says we're moving fast but ML doesn't detect vehicle - trust GPS
            return ("In Car/Bus", 75);
        }

        // Rule 3: Walking detection enhancement
        if (baseActivity == "Walking" || hasWalking)
        {
            if (speedValid && speed < WALKING_SPEED_MAX)
            {
                // Speed confirms walking
                var conf = Math.Max(baseConfidence, 80);
                // Check if it might be running
                if (speed > 1.5 && hasRunning)
                {
                    return ("Running", 75);
                }
                return ("Walking", conf);
            }
            if (!speedValid)
            {
                // No GPS data, trust ML with boosted confidence
                return ("Walking", Math.Max(baseConfidence, 70));
            }
        }

        // Rule 4: Running detection
        if (baseActivity == "Running" || hasRunning)
        {
            if (speedValid && speed > WALKING_SPEED_MAX && speed < RUNNING_SPEED_MAX)
            {
                return ("Running", Math.Max(baseConfidence, 85));
            }
            return ("Running", Math.Max(baseConfidence, 70));
        }

        // Rule 5: On Bicycle
        if (baseActivity == "On Bicycle" || hasBicycle)
        {
            return ("On Bicycle", Math.Max(baseConfidence, 75));
        }

        // Rule 6: Still/Stationary
        if (baseActivity == "Still" || hasStill)
        {
            if (speedValid && speed < 0.5)
            {
                return ("Standing", 90);
            }
            return ("Standing", Math.Max(baseConfidence, 70));
        }

        // Rule 7: Low confidence fallbacks
        if (baseConfidence < 40)
        {
            // Check GPS for hints
            if (speedValid)
            {
                if (speed < 0.3) return ("Standing", 60);
                if (speed < WALKING_SPEED_MAX) return ("Walking", 55);
                if (speed < RUNNING_SPEED_MAX) return ("Running", 55);
                return ("In Car/Bus", 60);
            }

            // Check for tilting (phone being held/moved)
            var hasTilting = allActivities.Any(a => a.Type == DetectedActivity.Tilting && a.Confidence > 40);
            if (hasTilting)
            {
                return ("Light Movement", 50);
            }
        }

        return (baseActivity, baseConfidence);
    }

    /// <summary>
    /// Convert Android activity type to our app activity names
    /// </summary>
    private string ConvertActivityType(int activityType)
    {
        return activityType switch
        {
            DetectedActivity.InVehicle => "In Car/Bus",
            DetectedActivity.OnBicycle => "On Bicycle",
            DetectedActivity.OnFoot => "Walking",
            DetectedActivity.Running => "Running",
            DetectedActivity.Walking => "Walking",
            DetectedActivity.Still => "Still",
            DetectedActivity.Tilting => "Tilting",
            DetectedActivity.Unknown => "Unknown",
            _ => "Unknown"
        };
    }

    public void Dispose()
    {
        StopAsync().Wait();
        _callback?.Dispose();
        _client?.Dispose();
    }
}

/// <summary>
/// Callback receiver for activity recognition updates
/// </summary>
public class ActivityRecognitionCallback : ActivityRecognitionResultCallback
{
    private readonly ActivityRecognitionService _service;

    public ActivityRecognitionCallback(ActivityRecognitionService service)
    {
        _service = service;
    }

    public override void OnActivityRecognitionResult(ActivityRecognitionResult result)
    {
        var activities = result.ProbableActivities;
        _service.OnActivityResult(activities);
    }

    public override void OnFailure(Java.Lang.Exception e)
    {
        System.Diagnostics.Debug.WriteLine($"Activity recognition error: {e.Message}");
    }
}

/// <summary>
/// Interface for activity recognition events
/// </summary>
public interface IActivityRecognitionListener
{
    void OnActivityDetected(string activity, int confidence);
}
