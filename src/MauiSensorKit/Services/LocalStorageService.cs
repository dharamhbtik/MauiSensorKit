using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MauiSensorKit;

/// <summary>
/// Service for local storage of sensor data batches.
/// </summary>
public sealed class LocalStorageService : ILocalStorageService, IDisposable
{
    private readonly SensorKitOptions _options;
    private readonly ILogger<LocalStorageService> _logger;
    private readonly SemaphoreSlim _manifestLock = new(1, 1);
    private bool _disposed;

    private string StoragePath => _options.LocalStoragePath ?? FileSystem.AppDataDirectory;
    private string ManifestPath => FileHelper.GetManifestPath(StoragePath);

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalStorageService"/> class.
    /// </summary>
    /// <param name="options">The sensor kit options.</param>
    /// <param name="logger">The logger instance.</param>
    public LocalStorageService(IOptions<SensorKitOptions> options, ILogger<LocalStorageService> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Ensure storage directory exists
        FileHelper.EnsureDirectoryExists(StoragePath);
    }

    /// <inheritdoc/>
    public async Task SaveBatchAsync(SensorDataBatch batch, CancellationToken cancellationToken = default)
    {
        if (batch == null) throw new ArgumentNullException(nameof(batch));

        var fileName = $"{_options.FileNamePrefix}_{batch.SessionId}_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}.json";
        var filePath = Path.Combine(StoragePath, fileName);

        try
        {
            // Serialize batch
            var json = JsonSerializer.Serialize(batch, SensorDataJsonContext.Default.SensorDataBatch);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);

            // Write to file
            await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);

            // Update manifest
            var entry = new BatchManifestEntry
            {
                FileName = fileName,
                SessionId = batch.SessionId,
                CreatedAt = DateTimeOffset.UtcNow,
                ReadingCount = batch.ReadingCount,
                IsUploaded = false,
                FileSizeBytes = bytes.Length
            };

            await AddToManifestAsync(entry, cancellationToken);

            _logger.LogDebug("Saved batch {FileName} ({Size} bytes)", fileName, bytes.Length);

            // Enforce storage limits
            await EnforceStorageLimitsAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving batch to {FilePath}", filePath);

            // Clean up partial file
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch { }

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchManifestEntry>> GetPendingBatchesAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await LoadManifestAsync(cancellationToken);
        return manifest.Batches.Where(b => !b.IsUploaded).ToList().AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<BatchManifestEntry>> GetAllBatchesAsync(CancellationToken cancellationToken = default)
    {
        var manifest = await LoadManifestAsync(cancellationToken);
        return manifest.Batches.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task<SensorDataBatch?> LoadBatchAsync(BatchManifestEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        var filePath = Path.Combine(StoragePath, entry.FileName);

        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Batch file not found: {FilePath}", filePath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var batch = JsonSerializer.Deserialize(json, SensorDataJsonContext.Default.SensorDataBatch);
            return batch;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading batch from {FilePath}", filePath);
            return null;
        }
    }

    /// <inheritdoc/>
    public async Task MarkBatchAsUploadedAsync(string fileName, CancellationToken cancellationToken = default)
    {
        await _manifestLock.WaitAsync(cancellationToken);
        try
        {
            var manifest = await LoadManifestInternalAsync(cancellationToken);

            var entry = manifest.Batches.FirstOrDefault(b => b.FileName == fileName);
            if (entry != null)
            {
                entry.IsUploaded = true;
                entry.UploadedAt = DateTimeOffset.UtcNow;
                manifest.LastUpdated = DateTimeOffset.UtcNow;

                await SaveManifestInternalAsync(manifest, cancellationToken);
                _logger.LogDebug("Marked batch {FileName} as uploaded", fileName);
            }
        }
        finally
        {
            _manifestLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteBatchAsync(string fileName, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(StoragePath, fileName);

        await _manifestLock.WaitAsync(cancellationToken);
        try
        {
            // Delete file
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting batch file {FilePath}", filePath);
                    return false;
                }
            }

            // Update manifest
            var manifest = await LoadManifestInternalAsync(cancellationToken);
            manifest.RemoveBatch(fileName);
            await SaveManifestInternalAsync(manifest, cancellationToken);

            _logger.LogDebug("Deleted batch {FileName}", fileName);
            return true;
        }
        finally
        {
            _manifestLock.Release();
        }
    }

    /// <inheritdoc/>
    public Task<long> GetStorageSizeInBytesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(FileHelper.GetDirectorySize(StoragePath, "*.json"));
    }

    /// <inheritdoc/>
    public async Task EnforceStorageLimitsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Check file count limit
            var files = Directory.GetFiles(StoragePath, "*.json")
                .Select(f => new FileInfo(f))
                .OrderBy(fi => fi.CreationTime)
                .ToList();

            // Remove oldest files if over count limit
            while (files.Count > _options.MaxLocalFileCount)
            {
                var oldest = files.First();
                files.RemoveAt(0);

                await DeleteBatchAsync(oldest.Name, cancellationToken);
                _logger.LogInformation("Deleted old batch {FileName} to enforce file count limit", oldest.Name);
            }

            // Check total size limit
            long maxSizeBytes = (long)_options.MaxLocalFileSizeMB * 1024 * 1024;
            long currentSize = files.Sum(f => f.Length);

            while (currentSize > maxSizeBytes && files.Count > 0)
            {
                var oldest = files.First();
                files.RemoveAt(0);

                await DeleteBatchAsync(oldest.Name, cancellationToken);
                currentSize -= oldest.Length;

                _logger.LogInformation(
                    "Deleted old batch {FileName} ({Size} MB) to enforce size limit",
                    oldest.Name,
                    oldest.Length / (1024.0 * 1024.0));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enforcing storage limits");
        }
    }

    /// <inheritdoc/>
    public async Task<int> GetPendingBatchCountAsync(CancellationToken cancellationToken = default)
    {
        var pending = await GetPendingBatchesAsync(cancellationToken);
        return pending.Count;
    }

    private async Task AddToManifestAsync(BatchManifestEntry entry, CancellationToken cancellationToken)
    {
        await _manifestLock.WaitAsync(cancellationToken);
        try
        {
            var manifest = await LoadManifestInternalAsync(cancellationToken);
            manifest.AddBatch(entry);
            await SaveManifestInternalAsync(manifest, cancellationToken);
        }
        finally
        {
            _manifestLock.Release();
        }
    }

    private async Task<SensorManifest> LoadManifestAsync(CancellationToken cancellationToken)
    {
        await _manifestLock.WaitAsync(cancellationToken);
        try
        {
            return await LoadManifestInternalAsync(cancellationToken);
        }
        finally
        {
            _manifestLock.Release();
        }
    }

    private async Task<SensorManifest> LoadManifestInternalAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(ManifestPath))
        {
            return new SensorManifest();
        }

        try
        {
            var json = await File.ReadAllTextAsync(ManifestPath, cancellationToken);
            var manifest = JsonSerializer.Deserialize(json, SensorDataJsonContext.Default.SensorManifest);
            return manifest ?? new SensorManifest();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading manifest, creating new one");
            return new SensorManifest();
        }
    }

    private async Task SaveManifestInternalAsync(SensorManifest manifest, CancellationToken cancellationToken)
    {
        try
        {
            var json = JsonSerializer.Serialize(manifest, SensorDataJsonContext.Default.SensorManifest);
            await File.WriteAllTextAsync(ManifestPath, json, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving manifest");
            throw;
        }
    }

    /// <inheritdoc/>
    public string GetExportDirectoryPath()
    {
        var exportPath = Path.Combine(StoragePath, "Exports");
        FileHelper.EnsureDirectoryExists(exportPath);
        return exportPath;
    }

    /// <inheritdoc/>
    public async Task<string> ExportToTextFileAsync(CancellationToken cancellationToken = default)
    {
        var exportPath = GetExportDirectoryPath();
        var fileName = $"SensorRecordings_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.txt";
        var filePath = Path.Combine(exportPath, fileName);

        try
        {
            var batches = await GetAllBatchesAsync(cancellationToken);
            var sb = new System.Text.StringBuilder();
            
            sb.AppendLine("============================================");
            sb.AppendLine("  MAUI SENSOR KIT - RECORDINGS EXPORT");
            sb.AppendLine($"  Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine("============================================");
            sb.AppendLine();
            sb.AppendLine($"Total Sessions: {batches.Count}");
            sb.AppendLine();

            foreach (var batch in batches.OrderBy(b => b.CreatedAt))
            {
                sb.AppendLine("--------------------------------------------");
                sb.AppendLine($"Session ID: {batch.SessionId}");
                sb.AppendLine($"Created: {batch.CreatedAt:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Readings: {batch.ReadingCount}");
                sb.AppendLine($"File Size: {FileHelper.FormatBytes(batch.FileSizeBytes)}");
                sb.AppendLine($"Uploaded: {(batch.IsUploaded ? "Yes" : "No")}");
                sb.AppendLine();
            }

            sb.AppendLine("============================================");
            sb.AppendLine("  END OF EXPORT");
            sb.AppendLine("============================================");

            await File.WriteAllTextAsync(filePath, sb.ToString(), cancellationToken);
            _logger.LogInformation("Exported text file: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to text file");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> ExportToZipAsync(CancellationToken cancellationToken = default)
    {
        var exportPath = GetExportDirectoryPath();
        var fileName = $"SensorRecordings_{DateTimeOffset.UtcNow:yyyyMMdd_HHmmss}.zip";
        var filePath = Path.Combine(exportPath, fileName);

        try
        {
            var batches = await GetAllBatchesAsync(cancellationToken);
            var tempDir = Path.Combine(StoragePath, $"temp_export_{Guid.NewGuid():N}");
            FileHelper.EnsureDirectoryExists(tempDir);

            // Create metadata file
            var metadataPath = Path.Combine(tempDir, "metadata.txt");
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("MAUI SENSOR KIT - ZIP EXPORT METADATA");
            sb.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            sb.AppendLine($"Total Sessions: {batches.Count}");
            sb.AppendLine();
            sb.AppendLine("Sessions:");
            foreach (var batch in batches.OrderBy(b => b.CreatedAt))
            {
                sb.AppendLine($"  - {batch.SessionId}: {batch.ReadingCount} readings ({batch.CreatedAt:yyyy-MM-dd HH:mm:ss})");
            }
            await File.WriteAllTextAsync(metadataPath, sb.ToString(), cancellationToken);

            // Copy all batch JSON files
            foreach (var batch in batches)
            {
                var sourcePath = Path.Combine(StoragePath, batch.FileName);
                var destPath = Path.Combine(tempDir, batch.FileName);
                if (File.Exists(sourcePath))
                {
                    File.Copy(sourcePath, destPath, overwrite: true);
                }
            }

            // Create ZIP archive
            System.IO.Compression.ZipFile.CreateFromDirectory(tempDir, filePath, 
                System.IO.Compression.CompressionLevel.Optimal, includeBaseDirectory: false);

            // Cleanup temp directory
            Directory.Delete(tempDir, recursive: true);

            _logger.LogInformation("Exported ZIP file: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting to ZIP");
            throw;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;

        _manifestLock.Dispose();
        _disposed = true;
    }
}
