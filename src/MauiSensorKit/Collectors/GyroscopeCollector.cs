using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Collector for gyroscope sensor data.
/// </summary>
public sealed class GyroscopeCollector : BaseSensorCollector<GyroscopeCollector>
{
    private string? _sessionId;

    /// <summary>
    /// Initializes a new instance of the <see cref="GyroscopeCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public GyroscopeCollector(ILogger<GyroscopeCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Gyroscope;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
        try
        {
            return Task.FromResult(Gyroscope.Default?.IsSupported ?? false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking gyroscope support");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Gyroscope collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;
            Gyroscope.Default.ReadingChanged += OnReadingChanged;
            Gyroscope.Default.Start(Options.MotionSensorSpeed);
            IsRunning = true;

            Logger.LogInformation("Gyroscope collector started with speed {Speed}", Options.MotionSensorSpeed);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting gyroscope collector");
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
            Gyroscope.Default.ReadingChanged -= OnReadingChanged;
            Gyroscope.Default.Stop();
            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Gyroscope collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping gyroscope collector");
        }

        return Task.CompletedTask;
    }

    private void OnReadingChanged(object? sender, GyroscopeChangedEventArgs e)
    {
        try
        {
            var reading = new GyroscopeReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                X = e.Reading.AngularVelocity.X,
                Y = e.Reading.AngularVelocity.Y,
                Z = e.Reading.AngularVelocity.Z,
                IsSimulated = DeviceInfo.Current?.DeviceType == DeviceType.Virtual
            };

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing gyroscope reading");
        }
    }
}
