#if ANDROID
using Android.Hardware;
using Android.Runtime;
#endif

using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Collector for magnetometer (compass) sensor data.
/// </summary>
public sealed class MagnetometerCollector : BaseSensorCollector<MagnetometerCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _magnetometerSensor;
    private MagnetometerListener? _listener;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="MagnetometerCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public MagnetometerCollector(ILogger<MagnetometerCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Magnetometer;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.MagneticField);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking magnetometer support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        return Task.FromResult(true);
#else
        Logger.LogWarning("Magnetometer not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Magnetometer collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _magnetometerSensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.MagneticField);

            if (_magnetometerSensor == null)
            {
                Logger.LogWarning("Magnetometer not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new MagnetometerListener(this);
            _sensorManager.RegisterListener(_listener, _magnetometerSensor, SensorDelay.Normal);
#elif IOS
            Logger.LogWarning("iOS magnetometer implementation not yet complete");
            return Task.CompletedTask;
#else
            Logger.LogWarning("Magnetometer not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Magnetometer collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting magnetometer collector");
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

            Logger.LogInformation("Magnetometer collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping magnetometer collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class MagnetometerListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly MagnetometerCollector _collector;

        public MagnetometerListener(MagnetometerCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count < 3) return;

            try
            {
                var reading = new MagnetometerReading
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
                _collector.Logger.LogError(ex, "Error processing magnetometer reading");
            }
        }
    }
#endif
}
