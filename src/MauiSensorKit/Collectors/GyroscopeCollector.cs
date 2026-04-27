#if ANDROID
using Android.Hardware;
using Android.Runtime;
#endif

using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Collector for gyroscope sensor data.
/// </summary>
public sealed class GyroscopeCollector : BaseSensorCollector<GyroscopeCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _gyroscopeSensor;
    private GyroscopeListener? _listener;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="GyroscopeCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public GyroscopeCollector(ILogger<GyroscopeCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Gyroscope;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Gyroscope);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking gyroscope support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        return Task.FromResult(true); // iOS gyroscope is commonly available
#else
        Logger.LogWarning("Gyroscope not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Gyroscope collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _gyroscopeSensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Gyroscope);

            if (_gyroscopeSensor == null)
            {
                Logger.LogWarning("Gyroscope not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new GyroscopeListener(this);
            _sensorManager.RegisterListener(_listener, _gyroscopeSensor, SensorDelay.Normal);
#elif IOS
            // iOS implementation using native APIs would go here
            Logger.LogWarning("iOS gyroscope implementation not yet complete");
            return Task.CompletedTask;
#else
            Logger.LogWarning("Gyroscope not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Gyroscope collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting gyroscope collector");
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

            Logger.LogInformation("Gyroscope collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping gyroscope collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class GyroscopeListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly GyroscopeCollector _collector;

        public GyroscopeListener(GyroscopeCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count < 3) return;

            try
            {
                var reading = new GyroscopeReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    X = e.Values[0],
                    Y = e.Values[1],
                    Z = e.Values[2],
                    IsSimulated = false
                };

                _collector.RaiseReading(reading);
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing gyroscope reading");
            }
        }
    }
#endif
}
