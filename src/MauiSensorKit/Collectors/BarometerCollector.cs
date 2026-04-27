#if ANDROID
using Android.Hardware;
using Android.Runtime;
#endif

using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Collector for barometer sensor data (atmospheric pressure).
/// </summary>
public sealed class BarometerCollector : BaseSensorCollector<BarometerCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _pressureSensor;
    private BarometerListener? _listener;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="BarometerCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public BarometerCollector(ILogger<BarometerCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Barometer;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Pressure);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking barometer support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        return Task.FromResult(false); // iOS doesn't expose barometer via public API
#else
        Logger.LogWarning("Barometer not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Barometer collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _pressureSensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Pressure);

            if (_pressureSensor == null)
            {
                Logger.LogWarning("Barometer not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new BarometerListener(this);
            _sensorManager.RegisterListener(_listener, _pressureSensor, SensorDelay.Normal);
#else
            Logger.LogWarning("Barometer not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Barometer collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting barometer collector");
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

            Logger.LogInformation("Barometer collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping barometer collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class BarometerListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly BarometerCollector _collector;

        public BarometerListener(BarometerCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count < 1) return;

            try
            {
                var reading = new BarometerReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    PressureHPa = e.Values[0],
                    IsSimulated = false
                };

                _collector.RaiseReading(reading);
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing barometer reading");
            }
        }
    }
#endif
}
