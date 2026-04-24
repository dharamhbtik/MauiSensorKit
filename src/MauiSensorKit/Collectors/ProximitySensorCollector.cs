#if ANDROID
using Android.Hardware;
using Android.Runtime;
#endif

#if IOS
using UIKit;
using Foundation;
#endif

using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Collector for proximity sensor data (detects nearby objects).
/// </summary>
public sealed class ProximitySensorCollector : BaseSensorCollector<ProximitySensorCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _proximitySensor;
    private ProximityListener? _listener;
#endif

#if IOS
    private NSObject? _observer;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="ProximitySensorCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public ProximitySensorCollector(ILogger<ProximitySensorCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.ProximitySensor;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Proximity);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking proximity sensor support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        // iOS proximity monitoring is available on devices with proximity sensor
        return Task.FromResult(UIDevice.CurrentDevice.ProximityMonitoringAvailable);
#else
        Logger.LogWarning("Proximity sensor not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Proximity sensor collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _proximitySensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Proximity);

            if (_proximitySensor == null)
            {
                Logger.LogWarning("Proximity sensor not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new ProximityListener(this, _proximitySensor.MaximumRange);
            _sensorManager.RegisterListener(_listener, _proximitySensor, SensorDelay.Normal);

#elif IOS
            if (!UIDevice.CurrentDevice.ProximityMonitoringAvailable)
            {
                Logger.LogWarning("Proximity monitoring not available on this iOS device");
                return Task.CompletedTask;
            }

            UIDevice.CurrentDevice.ProximityMonitoringEnabled = true;

            _observer = UIDevice.Notifications.ObserveProximityStateDidChange((sender, e) =>
            {
                bool isNear = UIDevice.CurrentDevice.ProximityState;
                double distance = isNear ? 0.0 : 100.0; // iOS only reports near/far, not exact distance

                var reading = new ProximitySensorReading
                {
                    DeviceId = DeviceId,
                    SessionId = _sessionId ?? string.Empty,
                    DistanceCm = distance,
                    IsSimulated = false
                };

                RaiseReading(reading);
            });

#else
            Logger.LogWarning("Proximity sensor not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Proximity sensor collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting proximity sensor collector");
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
#elif IOS
            UIDevice.CurrentDevice.ProximityMonitoringEnabled = false;
            _observer?.Dispose();
            _observer = null;
#endif

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Proximity sensor collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping proximity sensor collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class ProximityListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly ProximitySensorCollector _collector;
        private readonly float _maxRange;

        public ProximityListener(ProximitySensorCollector collector, float maxRange)
        {
            _collector = collector;
            _maxRange = maxRange;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count == 0) return;

            try
            {
                float distanceCm = e.Values[0];

                var reading = new ProximitySensorReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    DistanceCm = distanceCm,
                    IsSimulated = false
                };

                _collector.RaiseReading(reading);
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing proximity sensor reading");
            }
        }
    }
#endif
}
