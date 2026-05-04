using Microsoft.Extensions.Logging;
#if ANDROID
using Android.Content;
using Android.OS;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for battery status sensor data.
/// </summary>
public sealed class BatteryCollector : BaseSensorCollector<BatteryCollector>
{
    private string? _sessionId;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="BatteryCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public BatteryCollector(ILogger<BatteryCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Battery;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
        // Battery is always available on mobile devices
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Battery collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Subscribe to battery info changes
            Battery.BatteryInfoChanged += OnBatteryInfoChanged;

            // Emit initial reading
            EmitBatteryReading();

            // Start polling loop for periodic updates
            _ = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(Options.BatteryPollingInterval, _cancellationTokenSource.Token);
                        EmitBatteryReading();
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error in battery polling loop");
                    }
                }
            }, _cancellationTokenSource.Token);

            IsRunning = true;
            Logger.LogInformation("Battery collector started with interval {Interval}", Options.BatteryPollingInterval);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting battery collector");
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
            Battery.BatteryInfoChanged -= OnBatteryInfoChanged;

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Battery collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping battery collector");
        }

        return Task.CompletedTask;
    }

    private void OnBatteryInfoChanged(object? sender, BatteryInfoChangedEventArgs e)
    {
        EmitBatteryReading();
    }

    private void EmitBatteryReading()
    {
        try
        {
            var state = ConvertBatteryState(Battery.State);
            var powerSource = ConvertPowerSource(Battery.PowerSource);

            var reading = new BatteryReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                ChargeLevel = Battery.ChargeLevel,
                State = state,
                PowerSource = powerSource,
                IsSimulated = false
            };

#if ANDROID
            // Get additional battery information from Android BatteryManager
            FillAndroidBatteryInfo(reading);
#endif

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error emitting battery reading");
        }
    }

#if ANDROID
    private void FillAndroidBatteryInfo(BatteryReading reading)
    {
        try
        {
            var context = global::Android.App.Application.Context;
            if (context == null) return;

            var intentFilter = new IntentFilter(Intent.ActionBatteryChanged);
            var batteryStatus = context.RegisterReceiver(null, intentFilter);

            if (batteryStatus != null)
            {
                // Voltage in millivolts, convert to volts
                int voltageMv = batteryStatus.GetIntExtra(BatteryManager.ExtraVoltage, -1);
                if (voltageMv > 0)
                {
                    reading.VoltageVolts = voltageMv / 1000.0;
                }

                // Current in microamperes, convert to milliamperes
                int currentUa = batteryStatus.GetIntExtra(BatteryManager.ExtraCurrentAverage, -1);
                if (currentUa >= 0)
                {
                    reading.CurrentMilliAmps = currentUa / 1000.0;
                }

                // Temperature in tenths of degree Celsius
                int tempTenths = batteryStatus.GetIntExtra(BatteryManager.ExtraTemperature, -1);
                if (tempTenths > 0)
                {
                    reading.TemperatureCelsius = tempTenths / 10.0;
                }

                // Technology (Li-ion, Li-poly, etc.)
                string? technology = batteryStatus.GetStringExtra(BatteryManager.ExtraTechnology);
                reading.Technology = technology ?? "Unknown";

                // Health
                int health = batteryStatus.GetIntExtra(BatteryManager.ExtraHealth, -1);
                reading.Health = ConvertBatteryHealth(health);

                // Battery Capacity (if available)
                var batteryManager = context.GetSystemService(Context.BatteryService) as BatteryManager;
                if (batteryManager != null)
                {
                    long remainingEnergy = batteryManager.GetLongProperty(BatteryManager.BatteryPropertyChargeCounter);
                    if (remainingEnergy > 0)
                    {
                        reading.CapacityRemainingMWh = (int)(remainingEnergy / 1000); // Convert to mWh
                    }

                    long remainingCapacity = batteryManager.GetLongProperty(BatteryManager.BatteryPropertyCapacity);
                    if (remainingCapacity > 0)
                    {
                        reading.BatteryCapacityPercent = remainingCapacity / 100.0;
                    }
                }

                Logger.LogDebug("Android battery info collected: Voltage={Voltage}V, Current={Current}mA, Temp={Temp}C",
                    reading.VoltageVolts, reading.CurrentMilliAmps, reading.TemperatureCelsius);
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error getting Android battery info");
        }
    }

    private static BatteryHealth ConvertBatteryHealth(int health)
    {
        return health switch
        {
            BatteryHealth.Good => BatteryHealth.Good,
            BatteryHealth.Cold => BatteryHealth.Cold,
            BatteryHealth.Dead => BatteryHealth.Dead,
            BatteryHealth.Overheat => BatteryHealth.Overheat,
            BatteryHealth.OverVoltage => BatteryHealth.OverVoltage,
            BatteryHealth.UnspecifiedFailure => BatteryHealth.UnspecifiedFailure,
            _ => BatteryHealth.Unknown
        };
    }
#endif

    private static BatteryState ConvertBatteryState(Microsoft.Maui.Devices.BatteryState state)
    {
        return state switch
        {
            Microsoft.Maui.Devices.BatteryState.Charging => BatteryState.Charging,
            Microsoft.Maui.Devices.BatteryState.Discharging => BatteryState.Discharging,
            Microsoft.Maui.Devices.BatteryState.Full => BatteryState.Full,
            Microsoft.Maui.Devices.BatteryState.NotCharging => BatteryState.NotCharging,
            _ => BatteryState.Unknown
        };
    }

    private static BatteryPowerSource ConvertPowerSource(Microsoft.Maui.Devices.BatteryPowerSource source)
    {
        return source switch
        {
            Microsoft.Maui.Devices.BatteryPowerSource.Battery => BatteryPowerSource.Battery,
            Microsoft.Maui.Devices.BatteryPowerSource.AC => BatteryPowerSource.Ac,
            Microsoft.Maui.Devices.BatteryPowerSource.Usb => BatteryPowerSource.Usb,
            Microsoft.Maui.Devices.BatteryPowerSource.Wireless => BatteryPowerSource.Wireless,
            _ => BatteryPowerSource.Unknown
        };
    }
}
