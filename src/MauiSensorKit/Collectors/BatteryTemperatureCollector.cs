#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for battery temperature sensor data.
/// </summary>
public sealed class BatteryTemperatureCollector : BaseSensorCollector<BatteryTemperatureCollector>
{
    private string? _sessionId;
    private CancellationTokenSource? _cancellationTokenSource;

#if ANDROID
    private BroadcastReceiver? _receiver;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="BatteryTemperatureCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public BatteryTemperatureCollector(ILogger<BatteryTemperatureCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.BatteryTemperature;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        // Battery temperature is available via BatteryManager on Android
        return Task.FromResult(true);
#else
        // iOS does not expose battery temperature
        Logger.LogInformation("iOS does not expose battery temperature API");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Battery temperature collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

#if ANDROID
            // Register for battery changed events
            _receiver = new BatteryTempReceiver(this);
            var filter = new IntentFilter();
            filter.AddAction(Intent.ActionBatteryChanged);
            global::Android.App.Application.Context.RegisterReceiver(_receiver, filter);

            // Request an immediate update
            var batteryIntent = global::Android.App.Application.Context.RegisterReceiver(null, new IntentFilter(Intent.ActionBatteryChanged));
            if (batteryIntent != null)
            {
                ProcessBatteryIntent(batteryIntent);
            }

#elif IOS
            Logger.LogInformation("iOS does not expose battery temperature API");

            // Emit a simulated reading
            var reading = new BatteryTemperatureReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                TemperatureCelsius = 30.0, // Placeholder value
                IsSimulated = true
            };
            RaiseReading(reading);

            return Task.CompletedTask;
#else
            Logger.LogWarning("Battery temperature not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
            Logger.LogInformation("Battery temperature collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting battery temperature collector");
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

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Battery temperature collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping battery temperature collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    internal void ProcessBatteryIntent(Intent intent)
    {
        try
        {
            // EXTRA_TEMPERATURE is in tenths of a degree Celsius
            int temperatureTenths = intent.GetIntExtra(BatteryManager.ExtraTemperature, 0);
            double temperatureCelsius = temperatureTenths / 10.0;

            var reading = new BatteryTemperatureReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                TemperatureCelsius = temperatureCelsius,
                IsSimulated = false
            };

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing battery temperature");
        }
    }

    private class BatteryTempReceiver : BroadcastReceiver
    {
        private readonly BatteryTemperatureCollector _collector;

        public BatteryTempReceiver(BatteryTemperatureCollector collector)
        {
            _collector = collector;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action != Intent.ActionBatteryChanged) return;
            _collector.ProcessBatteryIntent(intent);
        }
    }
#endif
}
