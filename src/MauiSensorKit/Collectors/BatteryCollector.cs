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
                    catch (global::System.OperationCanceledException)
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
            reading = FillAndroidBatteryInfo(reading);
#endif

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error emitting battery reading");
        }
    }

#if ANDROID
    private BatteryReading FillAndroidBatteryInfo(BatteryReading reading)
    {
        try
        {
            var context = global::Android.App.Application.Context;
            if (context == null) return reading;

            var intentFilter = new IntentFilter(Intent.ActionBatteryChanged);
            var batteryStatus = context.RegisterReceiver(null, intentFilter);

            if (batteryStatus != null)
            {
                double? voltageVolts = reading.VoltageVolts;
                double? currentMilliAmps = reading.CurrentMilliAmps;
                double? temperatureCelsius = reading.TemperatureCelsius;
                string technology = reading.Technology;
                BatteryHealth health = reading.Health;
                int? capacityRemainingMWh = reading.CapacityRemainingMWh;
                double? batteryCapacityPercent = reading.BatteryCapacityPercent;

                // Voltage in millivolts, convert to volts
                int voltageMv = batteryStatus.GetIntExtra(BatteryManager.ExtraVoltage, -1);
                if (voltageMv > 0)
                {
                    voltageVolts = voltageMv / 1000.0;
                }

                // Current in microamperes is usually queried via BatteryManager property rather than intent extra
                // We will handle it with batteryManager below

                // Temperature in tenths of degree Celsius
                int tempTenths = batteryStatus.GetIntExtra(BatteryManager.ExtraTemperature, -1);
                if (tempTenths > 0)
                {
                    temperatureCelsius = tempTenths / 10.0;
                }

                // Technology (Li-ion, Li-poly, etc.)
                string? techStr = batteryStatus.GetStringExtra(BatteryManager.ExtraTechnology);
                if (techStr != null)
                {
                    technology = techStr;
                }

                // Health
                int healthInt = batteryStatus.GetIntExtra(BatteryManager.ExtraHealth, -1);
                health = ConvertBatteryHealth(healthInt);

                // Battery Capacity and Current (if available)
                var batteryManager = context.GetSystemService(Context.BatteryService) as BatteryManager;
                if (batteryManager != null)
                {
                    long remainingEnergy = batteryManager.GetLongProperty((int)global::Android.OS.BatteryProperty.ChargeCounter);
                    if (remainingEnergy > 0)
                    {
                        capacityRemainingMWh = (int)(remainingEnergy / 1000); // Convert to mWh
                    }

                    long remainingCapacity = batteryManager.GetLongProperty((int)global::Android.OS.BatteryProperty.Capacity);
                    if (remainingCapacity > 0)
                    {
                        batteryCapacityPercent = remainingCapacity / 100.0;
                    }

                    long currentUa = batteryManager.GetLongProperty((int)global::Android.OS.BatteryProperty.CurrentNow);
                    if (currentUa != 0 && currentUa != long.MinValue) // 0 could be valid but often means unavailable if not charging/discharging
                    {
                        // Some devices report inverted, some report positive for charging
                        currentMilliAmps = currentUa / 1000.0;
                    }
                }

                var updatedReading = reading with
                {
                    VoltageVolts = voltageVolts,
                    CurrentMilliAmps = currentMilliAmps,
                    TemperatureCelsius = temperatureCelsius,
                    Technology = technology,
                    Health = health,
                    CapacityRemainingMWh = capacityRemainingMWh,
                    BatteryCapacityPercent = batteryCapacityPercent
                };

                Logger.LogDebug("Android battery info collected: Voltage={Voltage}V, Current={Current}mA, Temp={Temp}C",
                    updatedReading.VoltageVolts, updatedReading.CurrentMilliAmps, updatedReading.TemperatureCelsius);
                    
                return updatedReading;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Error getting Android battery info");
        }
        
        return reading;
    }

    private static BatteryHealth ConvertBatteryHealth(int health)
    {
        var androidHealth = (global::Android.OS.BatteryHealth)health;
        return androidHealth switch
        {
            global::Android.OS.BatteryHealth.Good => BatteryHealth.Good,
            global::Android.OS.BatteryHealth.Cold => BatteryHealth.Cold,
            global::Android.OS.BatteryHealth.Dead => BatteryHealth.Dead,
            global::Android.OS.BatteryHealth.Overheat => BatteryHealth.Overheat,
            global::Android.OS.BatteryHealth.OverVoltage => BatteryHealth.OverVoltage,
            global::Android.OS.BatteryHealth.UnspecifiedFailure => BatteryHealth.UnspecifiedFailure,
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
