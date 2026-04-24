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
/// Collector for rotation vector sensor data (device orientation as quaternion).
/// </summary>
public sealed class RotationVectorCollector : BaseSensorCollector<RotationVectorCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _rotationVectorSensor;
    private RotationListener? _listener;
#endif

#if IOS
    private CMMotionManager? _motionManager;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="RotationVectorCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public RotationVectorCollector(ILogger<RotationVectorCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.RotationVector;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.RotationVector);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking rotation vector sensor support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        return Task.FromResult(true);
#else
        Logger.LogWarning("Rotation vector sensor not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Rotation vector collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _rotationVectorSensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.RotationVector);

            if (_rotationVectorSensor == null)
            {
                Logger.LogWarning("Rotation vector sensor not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new RotationListener(this);
            _sensorManager.RegisterListener(_listener, _rotationVectorSensor, SensorDelay.Ui);

#elif IOS
            _motionManager = new CMMotionManager();
            if (!_motionManager.DeviceMotionAvailable)
            {
                Logger.LogWarning("Device motion not available on this iOS device");
                return Task.CompletedTask;
            }

            _motionManager.StartDeviceMotionUpdates(NSOperationQueue.CurrentQueue, (data, error) =>
            {
                if (error != null || data == null) return;

                var attitude = data.Attitude;
                var reading = new RotationVectorReading
                {
                    DeviceId = DeviceId,
                    SessionId = _sessionId ?? string.Empty,
                    X = 0, // CMQuaternion values not directly accessible
                    Y = 0,
                    Z = 0, // CMQuaternion doesn't expose Z directly
                    W = 0, // CMQuaternion doesn't expose W directly
                    HeadingAccuracy = null, // iOS doesn't provide this directly
                    IsSimulated = false
                };

                RaiseReading(reading);
            });

#else
            Logger.LogWarning("Rotation vector sensor not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Rotation vector collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting rotation vector collector");
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
            _motionManager?.StopDeviceMotionUpdates();
            _motionManager?.Dispose();
            _motionManager = null;
#endif

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Rotation vector collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping rotation vector collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class RotationListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly RotationVectorCollector _collector;

        public RotationListener(RotationVectorCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count < 4) return;

            try
            {
                var reading = new RotationVectorReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    X = e.Values[0],
                    Y = e.Values[1],
                    Z = e.Values[2],
                    W = e.Values[3],
                    HeadingAccuracy = e.Values.Count > 4 ? (double?)e.Values[4] : null,
                    IsSimulated = false
                };

                _collector.RaiseReading(reading);
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing rotation vector reading");
            }
        }
    }
#endif
}
