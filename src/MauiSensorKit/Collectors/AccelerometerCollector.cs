namespace MauiSensorKit;

/// <summary>
/// Collector for accelerometer sensor data.
/// </summary>
public sealed class AccelerometerCollector : BaseSensorCollector<AccelerometerCollector>
{
    private string? _sessionId;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccelerometerCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public AccelerometerCollector(ILogger<AccelerometerCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Accelerometer;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
        try
        {
            return Task.FromResult(Accelerometer.Default?.IsSupported ?? false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking accelerometer support");
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Accelerometer collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;
            Accelerometer.Default.ReadingChanged += OnReadingChanged;
            Accelerometer.Default.Start(Options.MotionSensorSpeed);
            IsRunning = true;

            Logger.LogInformation("Accelerometer collector started with speed {Speed}", Options.MotionSensorSpeed);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting accelerometer collector");
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
            Accelerometer.Default.ReadingChanged -= OnReadingChanged;
            Accelerometer.Default.Stop();
            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Accelerometer collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping accelerometer collector");
        }

        return Task.CompletedTask;
    }

    private void OnReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        try
        {
            var reading = new AccelerometerReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                X = e.Reading.Acceleration.X,
                Y = e.Reading.Acceleration.Y,
                Z = e.Reading.Acceleration.Z,
                IsSimulated = DeviceInfo.Current?.DeviceType == DeviceType.Virtual
            };

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing accelerometer reading");
        }
    }
}
