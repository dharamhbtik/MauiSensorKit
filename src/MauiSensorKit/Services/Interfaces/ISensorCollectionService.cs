namespace MauiSensorKit;

/// <summary>
/// Service interface for managing sensor data collection.
/// </summary>
public interface ISensorCollectionService
{
    /// <summary>
    /// Gets a value indicating whether the service is currently collecting data.
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Gets the current session identifier.
    /// </summary>
    string? CurrentSessionId { get; }

    /// <summary>
    /// Gets the last availability report from the sensors.
    /// </summary>
    IReadOnlyDictionary<SensorType, SensorAvailabilityStatus>? LastAvailabilityReport { get; }

    /// <summary>
    /// Starts collecting sensor data from all enabled sensors.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops collecting sensor data.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync();

    /// <summary>
    /// Event raised when a sensor reading is recorded.
    /// </summary>
    event EventHandler<SensorReading>? ReadingRecorded;

    /// <summary>
    /// Event raised when sensor availability is checked.
    /// </summary>
    event EventHandler<SensorAvailabilityReport>? AvailabilityChecked;
}
