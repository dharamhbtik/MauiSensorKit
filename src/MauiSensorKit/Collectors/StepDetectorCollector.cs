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
/// Collector for step detector events (individual step detection).
/// </summary>
public sealed class StepDetectorCollector : BaseSensorCollector<StepDetectorCollector>
{
    private string? _sessionId;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _stepDetectorSensor;
    private StepDetectorListener? _listener;
#endif

#if IOS
    private CMPedometer? _pedometer;
    private long _lastSteps = -1;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="StepDetectorCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public StepDetectorCollector(ILogger<StepDetectorCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.StepDetector;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.StepDetector);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking step detector support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        return Task.FromResult(CMPedometer.IsStepCountingAvailable);
#else
        Logger.LogWarning("Step detector not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Step detector collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _stepDetectorSensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.StepDetector);

            if (_stepDetectorSensor == null)
            {
                Logger.LogWarning("Step detector not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new StepDetectorListener(this);
            _sensorManager.RegisterListener(_listener, _stepDetectorSensor, SensorDelay.Normal);

#elif IOS
            _pedometer = new CMPedometer();
            if (!CMPedometer.IsStepCountingAvailable)
            {
                Logger.LogWarning("Step detection not available on this iOS device");
                return Task.CompletedTask;
            }

            _lastSteps = -1;
            _pedometer.StartPedometerUpdates(NSDate.Now, (data, error) =>
            {
                if (error != null || data == null) return;

                long steps = (long)data.NumberOfSteps;
                if (_lastSteps >= 0 && steps > _lastSteps)
                {
                    // Steps increased, emit step detected event
                    var reading = new StepDetectorReading
                    {
                        DeviceId = DeviceId,
                        SessionId = _sessionId ?? string.Empty,
                        StepDetectedAt = DateTimeOffset.UtcNow,
                        IsSimulated = false
                    };

                    RaiseReading(reading);
                }
                _lastSteps = steps;
            });

#else
            Logger.LogWarning("Step detector not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Step detector collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting step detector collector");
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
            _pedometer?.StopPedometerUpdates();
            _pedometer?.Dispose();
            _pedometer = null;
#endif

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Step detector collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping step detector collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class StepDetectorListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly StepDetectorCollector _collector;

        public StepDetectorListener(StepDetectorCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Sensor?.Type != global::Android.Hardware.SensorType.StepDetector) return;

            try
            {
                var reading = new StepDetectorReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    StepDetectedAt = DateTimeOffset.UtcNow,
                    IsSimulated = false
                };

                _collector.RaiseReading(reading);
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing step detector reading");
            }
        }
    }
#endif
}
