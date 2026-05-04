using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;

namespace MauiSensorKit;

/// <summary>
/// Collector for barometer sensor data (atmospheric pressure).
/// </summary>
public sealed class BarometerCollector : BaseSensorCollector<BarometerCollector>
{
    private string? _sessionId;

    /// <summary>
    /// Initializes a new instance of the <see cref="BarometerCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public BarometerCollector(ILogger<BarometerCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Barometer;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
        return Task.FromResult(Barometer.Default.IsSupported);
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Barometer collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

            if (!Barometer.Default.IsSupported)
            {
                Logger.LogWarning("Barometer not available on this device");
                return Task.CompletedTask;
            }

            Barometer.Default.ReadingChanged += Barometer_ReadingChanged;
            Barometer.Default.Start(SensorSpeed.UI);

            IsRunning = true;
            Logger.LogInformation("Barometer collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting barometer collector");
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
            if (Barometer.Default.IsSupported)
            {
                Barometer.Default.Stop();
                Barometer.Default.ReadingChanged -= Barometer_ReadingChanged;
            }

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Barometer collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping barometer collector");
        }

        return Task.CompletedTask;
    }

    private void Barometer_ReadingChanged(object? sender, BarometerChangedEventArgs e)
    {
        try
        {
            var reading = new BarometerReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                PressureHPa = e.Reading.PressureInHectopascals,
                IsSimulated = false
            };

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing barometer reading");
        }
    }
}
