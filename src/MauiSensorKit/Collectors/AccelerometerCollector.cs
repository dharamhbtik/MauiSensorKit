#if ANDROID
using Android.Hardware;
using Android.Runtime;
#endif

using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Collector for accelerometer sensor data with significant change detection.
/// </summary>
public sealed class AccelerometerCollector : BaseSensorCollector<AccelerometerCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _accelerometerSensor;
    private AccelerometerListener? _listener;
    
    // Significant change detection
    private float _lastX, _lastY, _lastZ;
    private const float SignificantChangeThreshold = 0.5f; // m/s^2
    private DateTime _lastReadingTime = DateTime.MinValue;
    private readonly TimeSpan _minTimeBetweenReadings = TimeSpan.FromMilliseconds(100); // Max 10Hz
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="AccelerometerCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public AccelerometerCollector(ILogger<AccelerometerCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Accelerometer;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Accelerometer);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking accelerometer support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        return Task.FromResult(true);
#else
        Logger.LogWarning("Accelerometer not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Accelerometer collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _accelerometerSensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Accelerometer);

            if (_accelerometerSensor == null)
            {
                Logger.LogWarning("Accelerometer not available on this Android device");
                return Task.CompletedTask;
            }

            _lastX = _lastY = _lastZ = 0;
            _lastReadingTime = DateTime.MinValue;
            
            _listener = new AccelerometerListener(this);
            _sensorManager.RegisterListener(_listener, _accelerometerSensor, SensorDelay.Normal);
#elif IOS
            Logger.LogWarning("iOS accelerometer implementation not yet complete");
            return Task.CompletedTask;
#else
            Logger.LogWarning("Accelerometer not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Accelerometer collector started (significant change detection enabled)");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting accelerometer collector");
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task StopAsync()
    {
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }

        try
        {
#if ANDROID
            if (_sensorManager != null && _listener != null)
            {
                _sensorManager.UnregisterListener(_listener);
                _listener = null;
            }
#endif

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Accelerometer collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping accelerometer collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private bool IsSignificantChange(float x, float y, float z)
    {
        var now = DateTime.Now;
        
        // Rate limiting - don't record more than every 100ms
        if (now - _lastReadingTime < _minTimeBetweenReadings)
            return false;
        
        // Calculate magnitude change
        var deltaX = Math.Abs(x - _lastX);
        var deltaY = Math.Abs(y - _lastY);
        var deltaZ = Math.Abs(z - _lastZ);
        var totalDelta = deltaX + deltaY + deltaZ;
        
        // Check if change is significant
        if (totalDelta > SignificantChangeThreshold)
        {
            _lastX = x;
            _lastY = y;
            _lastZ = z;
            _lastReadingTime = now;
            return true;
        }
        
        return false;
    }

    private class AccelerometerListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly AccelerometerCollector _collector;

        public AccelerometerListener(AccelerometerCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count < 3) return;

            try
            {
                var x = e.Values[0];
                var y = e.Values[1];
                var z = e.Values[2];
                
                // Only record significant changes
                if (!_collector.IsSignificantChange(x, y, z))
                    return;

                var reading = new AccelerometerReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    X = x,
                    Y = y,
                    Z = z,
                    IsSimulated = false
                };

                _collector.RaiseReading(reading);
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing accelerometer reading");
            }
        }
    }
#endif
}
