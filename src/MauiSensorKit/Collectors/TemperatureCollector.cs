#if ANDROID
using Android.Hardware;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for temperature sensor data.
/// </summary>
public sealed class TemperatureCollector : BaseSensorCollector<TemperatureCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _tempSensor;
    private TempListener? _listener;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="TemperatureCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public TemperatureCollector(ILogger<TemperatureCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Temperature;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(SensorType.AmbientTemperature);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking temperature sensor support on Android");
            return Task.FromResult(false);
        }
#else
        // iOS does not expose ambient temperature sensor
        Logger.LogInformation("iOS does not expose ambient temperature API");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Temperature collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _tempSensor = _sensorManager?.GetDefaultSensor(SensorType.AmbientTemperature);

            if (_tempSensor == null)
            {
                Logger.LogWarning("Temperature sensor not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new TempListener(this);
            _sensorManager.RegisterListener(_listener, _tempSensor, SensorDelay.Normal);

            IsRunning = true;
            Logger.LogInformation("Temperature collector started");
#else
            Logger.LogInformation("iOS does not expose ambient temperature API. Returning simulated reading.");

            // iOS: Emit a single simulated reading and stop
            var reading = new TemperatureReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                TemperatureCelsius = 25.0, // Placeholder value
                Source = TemperatureSource.Ambient,
                IsSimulated = true
            };
            RaiseReading(reading);

            // Not actually running since we can't get real data
            return Task.CompletedTask;
#endif
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting temperature collector");
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

            Logger.LogInformation("Temperature collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping temperature collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class TempListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly TemperatureCollector _collector;

        public TempListener(TemperatureCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count == 0) return;

            try
            {
                float tempC = e.Values[0];

                var reading = new TemperatureReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    TemperatureCelsius = tempC,
                    Source = TemperatureSource.Ambient,
                    IsSimulated = false
                };

                _collector.RaiseReading(reading);
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing temperature reading");
            }
        }
    }
#endif
}
