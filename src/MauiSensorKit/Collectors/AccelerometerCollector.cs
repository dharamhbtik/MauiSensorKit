using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices.Sensors;

namespace MauiSensorKit;

/// <summary>
/// Collector for accelerometer sensor data with significant change detection.
/// </summary>
public sealed class AccelerometerCollector : BaseSensorCollector<AccelerometerCollector>
{
    private string? _sessionId;
    
    // Significant change detection
    private float _lastX, _lastY, _lastZ;
    private const float SignificantChangeThreshold = 0.5f; // m/s^2
    private DateTime _lastReadingTime = DateTime.MinValue;
    private readonly TimeSpan _minTimeBetweenReadings = TimeSpan.FromMilliseconds(100); // Max 10Hz

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
        return Task.FromResult(Accelerometer.Default.IsSupported);
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

            if (!Accelerometer.Default.IsSupported)
            {
                Logger.LogWarning("Accelerometer not available on this device");
                return Task.CompletedTask;
            }

            _lastX = _lastY = _lastZ = 0;
            _lastReadingTime = DateTime.MinValue;
            
            Accelerometer.Default.ReadingChanged += Accelerometer_ReadingChanged;
            Accelerometer.Default.Start(SensorSpeed.UI);

            IsRunning = true;
            Logger.LogInformation("Accelerometer collector started (significant change detection enabled)");
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
            if (Accelerometer.Default.IsSupported)
            {
                Accelerometer.Default.Stop();
                Accelerometer.Default.ReadingChanged -= Accelerometer_ReadingChanged;
            }

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

    private void Accelerometer_ReadingChanged(object? sender, AccelerometerChangedEventArgs e)
    {
        try
        {
            // Convert G to m/s^2 to match the expected format
            var x = (float)(e.Reading.Acceleration.X * 9.80665);
            var y = (float)(e.Reading.Acceleration.Y * 9.80665);
            var z = (float)(e.Reading.Acceleration.Z * 9.80665);
            
            // Only record significant changes
            if (!IsSignificantChange(x, y, z))
                return;

            var reading = new AccelerometerReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                X = x,
                Y = y,
                Z = z,
                IsSimulated = false
            };

            RaiseReading(reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing accelerometer reading");
        }
    }

    private bool IsSignificantChange(float x, float y, float z)
    {
        var now = DateTime.Now;
        
        // Rate limiting - don't record more than every 100ms
        if (now - _lastReadingTime < _minTimeBetweenReadings)
            return false;
        
        // Calculate magnitude change
        var deltaX = Math.Abs(x - _lastX);
        var deltaY = Math.Abs(y - _lastY);
        var deltaZ = Math.Abs(z - _lastZ);
        var totalDelta = deltaX + deltaY + deltaZ;
        
        // Check if change is significant
        if (totalDelta > SignificantChangeThreshold)
        {
            _lastX = x;
            _lastY = y;
            _lastZ = z;
            _lastReadingTime = now;
            return true;
        }
        
        return false;
    }
}
