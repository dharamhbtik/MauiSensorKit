#if ANDROID
using Android.Hardware;
using Android.Runtime;
#endif

#if IOS
using CoreMotion;
using Foundation;
#endif

using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Collector for gravity sensor data (platform-specific implementation).
/// </summary>
public sealed class GravitySensorCollector : BaseSensorCollector<GravitySensorCollector>
{
    private string? _sessionId;
    private CancellationTokenSource? _cancellationTokenSource;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _gravitySensor;
    private GravityListener? _listener;
#endif

#if IOS
    private CMMotionManager? _motionManager;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="GravitySensorCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public GravitySensorCollector(ILogger<GravitySensorCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.GravitySensor;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _gravitySensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Gravity);
            return Task.FromResult(_gravitySensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking gravity sensor support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        // CoreMotion device motion is available on iOS devices with motion coprocessor
        return Task.FromResult(true);
#else
        Logger.LogWarning("Gravity sensor not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override async Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Gravity sensor collector is already running");
            return;
        }

        try
        {
            _sessionId = sessionId;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _gravitySensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Gravity);

            if (_gravitySensor == null)
            {
                Logger.LogWarning("Gravity sensor not available on this Android device");
                return;
            }

            _listener = new GravityListener(this);
            _sensorManager.RegisterListener(_listener, _gravitySensor, SensorDelay.Ui);

#elif IOS
            _motionManager = new CMMotionManager();
            if (!_motionManager.DeviceMotionAvailable)
            {
                Logger.LogWarning("Device motion not available on this iOS device");
                return;
            }

            _motionManager.StartDeviceMotionUpdates(NSOperationQueue.CurrentQueue, (data, error) =>
            {
                if (error != null)
                {
                    Logger.LogError("Device motion error: {Error}", error.LocalizedDescription);
                    return;
                }

                if (data != null)
                {
                    var reading = new GravitySensorReading
                    {
                        DeviceId = DeviceId,
                        SessionId = _sessionId ?? string.Empty,
                        X = data.Gravity.X,
                        Y = data.Gravity.Y,
                        Z = data.Gravity.Z,
                        IsSimulated = false
                    };

                    RaiseReading(reading);
                }
            });

#else
            Logger.LogWarning("Gravity sensor not supported on this platform");
            await Task.CompletedTask;
            return;
#endif

            IsRunning = true;
            Logger.LogInformation("Gravity sensor collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting gravity sensor collector");
            throw;
        }
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
            _motionManager?.StopDeviceMotionUpdates();
            _motionManager?.Dispose();
            _motionManager = null;
#endif

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Gravity sensor collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping gravity sensor collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class GravityListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly GravitySensorCollector _collector;

        public GravityListener(GravitySensorCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy)
        {
        }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null) return;

            try
            {
                var reading = new GravitySensorReading
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
                _collector.Logger.LogError(ex, "Error processing gravity sensor reading");
            }
        }
    }
#endif
}
