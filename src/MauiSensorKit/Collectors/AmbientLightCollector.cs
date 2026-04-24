#if ANDROID
using Android.Hardware;
using Android.Runtime;
#endif

#if IOS
using UIKit;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for ambient light sensor data (illuminance in lux).
/// </summary>
public sealed class AmbientLightCollector : BaseSensorCollector<AmbientLightCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _lightSensor;
    private LightListener? _listener;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="AmbientLightCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public AmbientLightCollector(ILogger<AmbientLightCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.AmbientLight;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(SensorType.Light);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking ambient light sensor support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        // iOS does not expose the ambient light sensor directly
        // We use screen brightness as an approximation
        Logger.LogInformation("iOS: Using screen brightness as ambient light approximation");
        return Task.FromResult(true);
#else
        Logger.LogWarning("Ambient light sensor not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Ambient light collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _lightSensor = _sensorManager?.GetDefaultSensor(SensorType.Light);

            if (_lightSensor == null)
            {
                Logger.LogWarning("Ambient light sensor not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new LightListener(this);
            _sensorManager.RegisterListener(_listener, _lightSensor, SensorDelay.Normal);

#elif IOS
            // On iOS, we can't access the ambient light sensor directly
            // Using screen brightness as a very rough approximation
            // This is limited but better than nothing
            Logger.LogInformation("iOS: Ambient light sensor not directly accessible. Using screen brightness as approximation.");

            // Emit a single reading with simulated flag
            var reading = new AmbientLightReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                Lux = UIScreen.MainScreen.Brightness * 500, // Very rough approximation
                IsSimulated = true
            };
            RaiseReading(reading);

            // Note: iOS doesn't provide continuous ambient light updates via public API
            IsRunning = false;
            return Task.CompletedTask;

#else
            Logger.LogWarning("Ambient light sensor not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Ambient light collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting ambient light collector");
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

            Logger.LogInformation("Ambient light collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping ambient light collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class LightListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly AmbientLightCollector _collector;

        public LightListener(AmbientLightCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count == 0) return;

            try
            {
                float lux = e.Values[0];

                var reading = new AmbientLightReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    Lux = lux,
                    IsSimulated = false
                };

                _collector.RaiseReading(reading);
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing ambient light reading");
            }
        }
    }
#endif
}
