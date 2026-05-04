namespace MauiSensorKit;

/// <summary>
/// Service interface for automated capturing, batching, and exporting of sensor data.
/// </summary>
public interface ISensorRecordingService
{
    /// <summary>
    /// Gets a value indicating whether the background recording service is currently running.
    /// </summary>
    bool IsRecording { get; }

    /// <summary>
    /// Starts capturing enabled sensors and batching data periodically into local storage.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task StartRecordingAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops recording and flushes the remaining buffered data to local storage.
    /// </summary>
    Task StopRecordingAsync();

    /// <summary>
    /// Exports all locally recorded and stored sensor data into a compressed ZIP file.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The path to the generated ZIP file.</returns>
    Task<string> ExportRecordingsToZipAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports all locally recorded and stored sensor data into a human-readable text file.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The path to the generated text file.</returns>
    Task<string> ExportRecordingsToTextAsync(CancellationToken cancellationToken = default);
}
