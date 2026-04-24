namespace MauiSensorKit;

/// <summary>
/// Represents a batch of sensor readings for storage and upload.
/// </summary>
public sealed class SensorDataBatch
{
    /// <summary>
    /// Gets or sets the unique identifier for this batch.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the session identifier for this batch.
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Gets or sets the device identifier that created this batch.
    /// </summary>
    public required string DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the collection of sensor readings in this batch.
    /// </summary>
    public List<SensorReading> Readings { get; set; } = [];

    /// <summary>
    /// Gets or sets the timestamp when this batch was created.
    /// </summary>
    public DateTimeOffset BatchCreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether this batch has been uploaded.
    /// </summary>
    public bool IsUploaded { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when this batch was uploaded, if applicable.
    /// </summary>
    public DateTimeOffset? UploadedAt { get; set; }

    /// <summary>
    /// Gets the number of readings in this batch.
    /// </summary>
    public int ReadingCount => Readings.Count;

    /// <summary>
    /// Adds a reading to this batch.
    /// </summary>
    /// <param name="reading">The sensor reading to add.</param>
    public void AddReading(SensorReading reading)
    {
        Readings.Add(reading);
    }

    /// <summary>
    /// Adds multiple readings to this batch.
    /// </summary>
    /// <param name="readings">The sensor readings to add.</param>
    public void AddReadings(IEnumerable<SensorReading> readings)
    {
        Readings.AddRange(readings);
    }

    /// <summary>
    /// Marks this batch as uploaded.
    /// </summary>
    public void MarkAsUploaded()
    {
        IsUploaded = true;
        UploadedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Gets a summary of readings grouped by sensor type.
    /// </summary>
    public Dictionary<SensorType, int> GetSummaryByType()
    {
        return Readings
            .GroupBy(r => r.Type)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Returns a formatted string representation of the batch.
    /// </summary>
    public override string ToString()
    {
        var status = IsUploaded ? $" [Uploaded at {UploadedAt:HH:mm:ss}]" : " [Pending]";
        return $"Batch {Id:N}: {ReadingCount} readings{status}";
    }
}

/// <summary>
/// Represents a manifest entry for a stored batch file.
/// </summary>
public sealed class BatchManifestEntry
{
    /// <summary>
    /// Gets or sets the file name of the batch.
    /// </summary>
    public required string FileName { get; set; }

    /// <summary>
    /// Gets or sets the session identifier.
    /// </summary>
    public required string SessionId { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the batch was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the number of readings in the batch.
    /// </summary>
    public int ReadingCount { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the batch has been uploaded.
    /// </summary>
    public bool IsUploaded { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the batch was uploaded.
    /// </summary>
    public DateTimeOffset? UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets the file size in bytes.
    /// </summary>
    public long FileSizeBytes { get; set; }
}

/// <summary>
/// Represents the manifest file containing all stored batch information.
/// </summary>
public sealed class SensorManifest
{
    /// <summary>
    /// Gets or sets the version of the manifest format.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the timestamp when the manifest was last updated.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the collection of batch manifest entries.
    /// </summary>
    public List<BatchManifestEntry> Batches { get; set; } = [];

    /// <summary>
    /// Gets the total number of batches in the manifest.
    /// </summary>
    public int TotalBatches => Batches.Count;

    /// <summary>
    /// Gets the number of pending (non-uploaded) batches.
    /// </summary>
    public int PendingBatches => Batches.Count(b => !b.IsUploaded);

    /// <summary>
    /// Gets the total storage size in bytes.
    /// </summary>
    public long TotalStorageSizeBytes => Batches.Sum(b => b.FileSizeBytes);

    /// <summary>
    /// Adds a new batch entry to the manifest.
    /// </summary>
    public void AddBatch(BatchManifestEntry entry)
    {
        Batches.Add(entry);
        LastUpdated = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Removes a batch entry from the manifest.
    /// </summary>
    public bool RemoveBatch(string fileName)
    {
        var batch = Batches.FirstOrDefault(b => b.FileName == fileName);
        if (batch != null)
        {
            Batches.Remove(batch);
            LastUpdated = DateTimeOffset.UtcNow;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Marks a batch as uploaded.
    /// </summary>
    public bool MarkAsUploaded(string fileName)
    {
        var batch = Batches.FirstOrDefault(b => b.FileName == fileName);
        if (batch != null)
        {
            batch.IsUploaded = true;
            batch.UploadedAt = DateTimeOffset.UtcNow;
            LastUpdated = DateTimeOffset.UtcNow;
            return true;
        }
        return false;
    }
}
