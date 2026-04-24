using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Abstract base class for all sensor collectors.
/// </summary>
public abstract class BaseSensorCollector : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Logger instance for the collector.
    /// </summary>
    protected readonly ILogger Logger;

    /// <summary>
    /// Configuration options for the sensor kit.
    /// </summary>
    protected readonly SensorKitOptions Options;

    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSensorCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    protected BaseSensorCollector(ILogger logger, SensorKitOptions options)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets the type of sensor this collector handles.
    /// </summary>
    public abstract SensorType SensorType { get; }

    /// <summary>
    /// Gets a value indicating whether the collector is currently running.
    /// </summary>
    public bool IsRunning { get; protected set; }

    /// <summary>
    /// Event raised when a new sensor reading is available.
    /// </summary>
    public event EventHandler<SensorReading>? ReadingAvailable;

    /// <summary>
    /// Gets the device identifier from persisted preferences.
    /// </summary>
    protected string DeviceId => GetOrCreateDeviceId();

    /// <summary>
    /// Checks if this sensor is supported on the current device.
    /// </summary>
    /// <returns>A task that returns true if the sensor is supported; otherwise, false.</returns>
    public abstract Task<bool> IsSupportedAsync();

    /// <summary>
    /// Starts collecting sensor data.
    /// </summary>
    /// <param name="sessionId">The session identifier for the data collection session.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task StartAsync(string sessionId, CancellationToken cancellationToken);

    /// <summary>
    /// Stops collecting sensor data.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public abstract Task StopAsync();

    /// <summary>
    /// Raises the <see cref="ReadingAvailable"/> event.
    /// </summary>
    /// <param name="reading">The sensor reading to raise.</param>
    protected void RaiseReading(SensorReading reading)
    {
        if (reading == null)
            throw new ArgumentNullException(nameof(reading));

        try
        {
            ReadingAvailable?.Invoke(this, reading);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error raising ReadingAvailable event for {SensorType}", SensorType);
        }
    }

    private static string GetOrCreateDeviceId()
    {
        const string key = "MauiSensorKit_DeviceId";

        var existingId = Preferences.Default.Get<string?>(key, null);
        if (!string.IsNullOrEmpty(existingId))
        {
            return existingId;
        }

        var newId = Guid.NewGuid().ToString("N");
        Preferences.Default.Set(key, newId);
        return newId;
    }

    /// <summary>
    /// Throws an exception indicating that a hardware-gated sensor is not supported.
    /// </summary>
    /// <param name="sensorName">The name of the sensor.</param>
    /// <param name="reason">The reason why it's not supported.</param>
    protected void ThrowNotSupported(string sensorName, string reason)
    {
        throw new SensorNotSupportedException($"{sensorName} is not supported: {reason}");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        if (IsRunning)
        {
            try
            {
                StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error stopping collector during dispose");
            }
        }

        _disposed = true;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (IsRunning)
        {
            try
            {
                await StopAsync();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error stopping collector during async dispose");
            }
        }

        _disposed = true;
    }
}

/// <summary>
/// Generic base class for typed sensor collectors.
/// </summary>
/// <typeparam name="TLogger">The logger type.</typeparam>
public abstract class BaseSensorCollector<TLogger> : BaseSensorCollector where TLogger : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSensorCollector{TLogger}"/> class.
    /// </summary>
    /// <param name="logger">The typed logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    protected BaseSensorCollector(ILogger<TLogger> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }
}

/// <summary>
/// Exception thrown when a sensor operation is not supported.
/// </summary>
public class SensorNotSupportedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SensorNotSupportedException"/> class.
    /// </summary>
    public SensorNotSupportedException() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SensorNotSupportedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SensorNotSupportedException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="SensorNotSupportedException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public SensorNotSupportedException(string message, Exception innerException) : base(message, innerException) { }
}
