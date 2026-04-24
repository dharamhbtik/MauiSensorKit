using Microsoft.Extensions.Logging;

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
        try
        {
            return Task.FromResult(Magnetometer.Default?.IsSupported ?? false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking magnetometer support");
            return Task.FromResult(false);
        }
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
            Magnetometer.Default.ReadingChanged += OnReadingChanged;
            Magnetometer.Default.Start(Options.MotionSensorSpeed);
            IsRunning = true;

            Logger.LogInformation("Magnetometer collector started with speed {Speed}", Options.MotionSensorSpeed);
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
            Magnetometer.Default.ReadingChanged -= OnReadingChanged;
            Magnetometer.Default.Stop();
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

    private void OnReadingChanged(object? sender, MagnetometerChangedEventArgs e)
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
                IsSimulated = DeviceInfo.Current?.DeviceType == DeviceType.Virtual
            };

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing magnetometer reading");
        }
    }
}
