#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for Hall effect sensor data (magnetic cover/flip case detection).
/// Note: The Hall sensor is not directly exposed via standard Android/iOS APIs.
/// This collector uses dock event detection as an approximation on Android.
/// </summary>
public sealed class HallSensorCollector : BaseSensorCollector<HallSensorCollector>
{
    private string? _sessionId;

#if ANDROID
    private BroadcastReceiver? _receiver;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="HallSensorCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public HallSensorCollector(ILogger<HallSensorCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.HallSensor;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        // Hall sensor is often available via dock events on Android
        // but there's no direct API to check availability
        Logger.LogInformation("Hall sensor detection uses dock events as approximation on Android");
        return Task.FromResult(true);
#elif IOS
        // iOS does not expose Hall sensor
        Logger.LogInformation("iOS does not expose Hall sensor API");
        return Task.FromResult(false);
#else
        Logger.LogWarning("Hall sensor not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Hall sensor collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            // Use dock event detection as an approximation for magnetic cover detection
            // This is not a direct Hall sensor reading but provides similar functionality
            Logger.LogInformation("Hall sensor collector started using dock event detection as approximation.");
            Logger.LogInformation("Note: Direct Hall sensor API is not available on Android.");

            _receiver = new HallBroadcastReceiver(this);
            var filter = new IntentFilter();
            filter.AddAction(Intent.ActionDockEvent);
            global::Android.App.Application.Context.RegisterReceiver(_receiver, filter);

            // Emit initial reading (unknown state)
            var reading = new HallSensorReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                IsCoverClosed = false, // Unknown, assume open
                IsSimulated = true // This is an approximation
            };
            RaiseReading(reading);

#elif IOS
            Logger.LogInformation("iOS does not expose Hall sensor API");

            // Emit simulated reading
            var reading = new HallSensorReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                IsCoverClosed = false,
                IsSimulated = true
            };
            RaiseReading(reading);

            return Task.CompletedTask;
#else
            Logger.LogWarning("Hall sensor not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting Hall sensor collector");
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
            if (_receiver != null)
            {
                try
                {
                    global::Android.App.Application.Context.UnregisterReceiver(_receiver);
                }
                catch { }
                _receiver = null;
            }
#endif

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Hall sensor collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping Hall sensor collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    private class HallBroadcastReceiver : BroadcastReceiver
    {
        private readonly HallSensorCollector _collector;

        public HallBroadcastReceiver(HallSensorCollector collector)
        {
            _collector = collector;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != Intent.ActionDockEvent) return;

            try
            {
                int dockState = intent.GetIntExtra(Intent.ExtraDockState, (int)DockState.Undocked);
                bool isCoverClosed = dockState != (int)DockState.Undocked;

                var reading = new HallSensorReading
                {
                    DeviceId = _collector.DeviceId,
                    SessionId = _collector._sessionId ?? string.Empty,
                    IsCoverClosed = isCoverClosed,
                    IsSimulated = true // This is an approximation via dock events
                };

                _collector.RaiseReading(reading);
                _collector.Logger.LogDebug("Hall sensor (dock) state changed: {State}", isCoverClosed ? "Closed" : "Open");
            }
            catch (Exception ex)
            {
                _collector.Logger.LogError(ex, "Error processing Hall sensor event");
            }
        }
    }
#endif
}
