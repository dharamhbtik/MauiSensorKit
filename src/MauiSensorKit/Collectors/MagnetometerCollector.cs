using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;

namespace MauiSensorKit;

/// <summary>
/// Collector for magnetometer (compass) sensor data.
/// </summary>
public sealed class MagnetometerCollector : BaseSensorCollector<MagnetometerCollector>
{
    private string? _sessionId;

    /// <summary>
    /// Initializes a new instance of the <see cref="MagnetometerCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public MagnetometerCollector(ILogger<MagnetometerCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Magnetometer;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
        return Task.FromResult(Magnetometer.Default.IsSupported);
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Magnetometer collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

            if (!Magnetometer.Default.IsSupported)
            {
                Logger.LogWarning("Magnetometer not available on this device");
                return Task.CompletedTask;
            }

            Magnetometer.Default.ReadingChanged += Magnetometer_ReadingChanged;
            Magnetometer.Default.Start(SensorSpeed.UI);

            IsRunning = true;
            Logger.LogInformation("Magnetometer collector started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting magnetometer collector");
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
            if (Magnetometer.Default.IsSupported)
            {
                Magnetometer.Default.Stop();
                Magnetometer.Default.ReadingChanged -= Magnetometer_ReadingChanged;
            }

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Magnetometer collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping magnetometer collector");
        }

        return Task.CompletedTask;
    }

    private void Magnetometer_ReadingChanged(object? sender, MagnetometerChangedEventArgs e)
    {
        try
        {
            var reading = new MagnetometerReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                X = e.Reading.MagneticField.X,
                Y = e.Reading.MagneticField.Y,
                Z = e.Reading.MagneticField.Z,
                IsSimulated = false
            };

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing magnetometer reading");
        }
    }
}
