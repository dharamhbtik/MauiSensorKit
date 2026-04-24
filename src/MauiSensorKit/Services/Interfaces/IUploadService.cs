namespace MauiSensorKit;

/// <summary>
/// Service interface for uploading sensor data to a remote API.
/// </summary>
public interface IUploadService
{
    /// <summary>
    /// Processes all pending uploads.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the number of batches successfully uploaded.</returns>
    Task<int> ProcessPendingUploadsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a specific batch.
    /// </summary>
    /// <param name="batch">The batch to upload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if uploaded successfully; otherwise, false.</returns>
    Task<bool> UploadBatchAsync(SensorDataBatch batch, CancellationToken cancellationToken = default);
}
