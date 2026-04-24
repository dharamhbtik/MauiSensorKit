namespace MauiSensorKit;

/// <summary>
/// Service interface for local storage of sensor data batches.
/// </summary>
public interface ILocalStorageService
{
    /// <summary>
    /// Saves a batch of sensor readings to local storage.
    /// </summary>
    /// <param name="batch">The batch to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SaveBatchAsync(SensorDataBatch batch, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending (non-uploaded) batches.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns a list of pending batch manifest entries.</returns>
    Task<IReadOnlyList<BatchManifestEntry>> GetPendingBatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all stored batches (including uploaded ones).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns a list of all batch manifest entries.</returns>
    Task<IReadOnlyList<BatchManifestEntry>> GetAllBatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a batch from its file.
    /// </summary>
    /// <param name="entry">The batch manifest entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the loaded batch, or null if not found.</returns>
    Task<SensorDataBatch?> LoadBatchAsync(BatchManifestEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a batch as uploaded in the manifest.
    /// </summary>
    /// <param name="fileName">The file name of the batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkBatchAsUploadedAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a batch file and removes it from the manifest.
    /// </summary>
    /// <param name="fileName">The file name of the batch.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully; otherwise, false.</returns>
    Task<bool> DeleteBatchAsync(string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total storage size in bytes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the total size in bytes.</returns>
    Task<long> GetStorageSizeInBytesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforces storage limits by deleting oldest files if necessary.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task EnforceStorageLimitsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of pending (non-uploaded) batches.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that returns the pending batch count.</returns>
    Task<int> GetPendingBatchCountAsync(CancellationToken cancellationToken = default);
}
