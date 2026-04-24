#if ANDROID
using Android.Hardware;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for humidity sensor data (relative humidity percentage).
/// </summary>
public sealed class HumidityCollector : BaseSensorCollector<HumidityCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _humiditySensor;
    private HumidityListener? _listener;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="HumidityCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public HumidityCollector(ILogger<HumidityCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Humidity;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(SensorType.RelativeHumidity);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking humidity sensor support on Android");
            return Task.FromResult(false);
        }
#else
        // iOS does not expose humidity sensor
        Logger.LogInformation("iOS does not expose humidity sensor API");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Humidity collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _humiditySensor = _sensorManager?.GetDefaultSensor(SensorType.RelativeHumidity);

            if (_humiditySensor == null)
            {
                Logger.LogWarning("Humidity sensor not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new HumidityListener(this);
            _sensorManager.RegisterListener(_listener, _humiditySensor, SensorDelay.Normal);

            IsRunning = true;
            Logger.LogInformation("Humidity collector started");
#else
            Logger.LogInformation("iOS does not expose humidity sensor API. Returning simulated reading.");

            // iOS: Emit a single simulated reading
            var reading = new HumidityReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                RelativeHumidityPercent = 50.0, // Placeholder value
                IsSimulated = true
            };
            RaiseReading(reading);

            return Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting humidity collector");
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

            Logger.LogInformation("Humidity collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping humidity collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class HumidityListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly HumidityCollector _collector;

        public HumidityListener(HumidityCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count == 0) return;

            try
            {
                float humidity = e.Values[0];

                var reading = new HumidityReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    RelativeHumidityPercent = humidity,
                    IsSimulated = false
                };

                _collector.RaiseReading(reading);
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing humidity reading");
            }
        }
    }
#endif
}
