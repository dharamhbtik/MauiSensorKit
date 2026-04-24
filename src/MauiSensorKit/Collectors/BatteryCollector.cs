using Microsoft.Extensions.Logging;

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

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error emitting battery reading");
        }
    }

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
