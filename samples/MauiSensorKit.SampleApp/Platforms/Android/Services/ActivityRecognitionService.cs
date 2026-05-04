using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MauiSensorKit.SampleApp.Platforms.Android.Services;

/// <summary>
/// Dummy activity recognition since Google Play Services Location removed the callback APIs.
/// </summary>
public class ActivityRecognitionService : Java.Lang.Object
{
    private bool _isRunning;

    // GPS speed tracking for vehicle detection enhancement
    private double _currentSpeedMps;
    private DateTime _lastSpeedUpdate = DateTime.MinValue;

    public bool IsRunning => _isRunning;

    public void Initialize(Context context)
    {
    }

    public Task<bool> StartAsync(IActivityRecognitionListener listener)
    {
        _isRunning = true;
        // In a real app, use PendingIntent to handle ActivityRecognition API
        return Task.FromResult(true);
    }

    public Task StopAsync()
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    public void UpdateGpsSpeed(double speedMps)
    {
        _currentSpeedMps = speedMps;
        _lastSpeedUpdate = DateTime.Now;
    }

    public void Dispose()
    {
        StopAsync().Wait();
    }
}

/// <summary>
/// Interface for activity recognition events
/// </summary>
public interface IActivityRecognitionListener
{
    void OnActivityDetected(string activity, int confidence);
}
