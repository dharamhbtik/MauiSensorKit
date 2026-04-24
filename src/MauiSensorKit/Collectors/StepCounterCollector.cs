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
/// Collector for step counter sensor data (cumulative steps since reboot).
/// </summary>
public sealed class StepCounterCollector : BaseSensorCollector<StepCounterCollector>
{
    private string? _sessionId;
    private long _lastStepCount = -1;

#if ANDROID
    private SensorManager? _sensorManager;
    private Sensor? _stepCounterSensor;
    private StepCounterListener? _listener;
#endif

#if IOS
    private CMPedometer? _pedometer;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="StepCounterCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public StepCounterCollector(ILogger<StepCounterCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.StepCounter;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            var sensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.StepCounter);
            return Task.FromResult(sensor != null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking step counter support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        return Task.FromResult(CMPedometer.IsStepCountingAvailable);
#else
        Logger.LogWarning("Step counter not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Step counter collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;
            _lastStepCount = -1;

#if ANDROID
            _sensorManager ??= global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as SensorManager;
            _stepCounterSensor = _sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.StepCounter);

            if (_stepCounterSensor == null)
            {
                Logger.LogWarning("Step counter not available on this Android device");
                return Task.CompletedTask;
            }

            _listener = new StepCounterListener(this);
            _sensorManager.RegisterListener(_listener, _stepCounterSensor, SensorDelay.Normal);

#elif IOS
            _pedometer = new CMPedometer();
            if (!CMPedometer.IsStepCountingAvailable)
            {
                Logger.LogWarning("Step counting not available on this iOS device");
                return Task.CompletedTask;
            }

            _pedometer.StartPedometerUpdates(NSDate.Now, (data, error) =>
            {
                if (error != null || data == null) return;

                long steps = (long)data.NumberOfSteps;
                if (_lastStepCount != steps)
                {
                    _lastStepCount = steps;

                    var reading = new StepCounterReading
                    {
                        DeviceId = DeviceId,
                        SessionId = _sessionId ?? string.Empty,
                        TotalSteps = steps,
                        IsSimulated = false
                    };

                    RaiseReading(reading);
                }
            });

#else
            Logger.LogWarning("Step counter not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Step counter collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting step counter collector");
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
            _lastStepCount = -1;

            Logger.LogInformation("Step counter collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping step counter collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class StepCounterListener : Java.Lang.Object, ISensorEventListener
    {
        private readonly StepCounterCollector _collector;

        public StepCounterListener(StepCounterCollector collector)
        {
            _collector = collector;
        }

        public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Values == null || e.Values.Count == 0) return;

            try
            {
                long steps = (long)e.Values[0];
                if (_collector._lastStepCount != steps)
                {
                    _collector._lastStepCount = steps;

                    var reading = new StepCounterReading
                    {
                        DeviceId = _collector.DeviceId,
                        SessionId = _collector._sessionId ?? string.Empty,
                        TotalSteps = steps,
                        IsSimulated = false
                    };

                    _collector.RaiseReading(reading);
                }
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing step counter reading");
            }
        }
    }
#endif
}
