#if ANDROID
using Android.Hardware;
using Android.Runtime;
#endif

#if IOS
using CoreMotion;
using Foundation;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for linear acceleration sensor data (acceleration excluding gravity).
/// </summary>
public sealed class LinearAccelerationCollector : BaseSensorCollector<LinearAccelerationCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _linearAccelSensor;
    private LinearAccelListener? _listener;
#endif

#if IOS
    private CMMotionManager? _motionManager;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="LinearAccelerationCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public LinearAccelerationCollector(ILogger<LinearAccelerationCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.LinearAcceleration;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(SensorType.LinearAcceleration);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking linear acceleration sensor support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        return Task.FromResult(true);
#else
        Logger.LogWarning("Linear acceleration sensor not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Linear acceleration collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _linearAccelSensor = _sensorManager?.GetDefaultSensor(SensorType.LinearAcceleration);

            if (_linearAccelSensor == null)
            {
                Logger.LogWarning("Linear acceleration sensor not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new LinearAccelListener(this);
            _sensorManager.RegisterListener(_listener, _linearAccelSensor, SensorDelay.Ui);

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

                var reading = new LinearAccelerationReading
                {
                    DeviceId = DeviceId,
                    SessionId = _sessionId ?? string.Empty,
                    X = data.UserAcceleration.X,
                    Y = data.UserAcceleration.Y,
                    Z = data.UserAcceleration.Z,
                    IsSimulated = false
                };

                RaiseReading(reading);
            });

#else
            Logger.LogWarning("Linear acceleration sensor not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Linear acceleration collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting linear acceleration collector");
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

            Logger.LogInformation("Linear acceleration collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping linear acceleration collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class LinearAccelListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly LinearAccelerationCollector _collector;

        public LinearAccelListener(LinearAccelerationCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null) return;

            try
            {
                var reading = new LinearAccelerationReading
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
                _collector.Logger.LogError(ex, "Error processing linear acceleration reading");
            }
        }
    }
#endif
}
