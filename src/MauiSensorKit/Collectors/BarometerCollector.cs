using Microsoft.Extensions.Logging;

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
        try
        {
            return Task.FromResult(Barometer.Default?.IsSupported ?? false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking barometer support");
            return Task.FromResult(false);
        }
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
            Barometer.Default.ReadingChanged += OnReadingChanged;
            Barometer.Default.Start(Options.SlowSensorPollingInterval);
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
            Barometer.Default.ReadingChanged -= OnReadingChanged;
            Barometer.Default.Stop();
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

    private void OnReadingChanged(object? sender, BarometerChangedEventArgs e)
    {
        try
        {
            var reading = new BarometerReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                PressureHPa = e.Reading.PressureInHectopascals,
                IsSimulated = DeviceInfo.Current?.DeviceType == DeviceType.Virtual
            };

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing barometer reading");
        }
    }
}
