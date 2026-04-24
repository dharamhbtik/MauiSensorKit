namespace MauiSensorKit;

/// <summary>
/// Helper class for file operations.
/// </summary>
public static class FileHelper
{
    /// <summary>
    /// Gets the default storage directory for sensor data.
    /// </summary>
    public static string GetDefaultStoragePath()
    {
        return FileSystem.AppDataDirectory;
    }

    /// <summary>
    /// Gets the full path for a sensor data batch file.
    /// </summary>
    /// <param name="prefix">The file name prefix.</param>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="timestamp">The timestamp for the file.</param>
    /// <returns>The full file path.</returns>
    public static string GetBatchFilePath(string prefix, string sessionId, DateTimeOffset timestamp)
    {
        var fileName = $"{prefix}_{sessionId}_{timestamp:yyyyMMdd_HHmmss}.json";
        return Path.Combine(GetDefaultStoragePath(), fileName);
    }

    /// <summary>
    /// Gets the full path for the manifest file.
    /// </summary>
    /// <param name="storagePath">The storage directory path.</param>
    /// <returns>The full manifest file path.</returns>
    public static string GetManifestPath(string? storagePath = null)
    {
        var basePath = storagePath ?? GetDefaultStoragePath();
        return Path.Combine(basePath, "sensor_manifest.json");
    }

    /// <summary>
    /// Gets the size of a file in bytes.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>The file size in bytes, or 0 if the file doesn't exist.</returns>
    public static long GetFileSize(string filePath)
    {
        if (!File.Exists(filePath))
            return 0;

        return new FileInfo(filePath).Length;
    }

    /// <summary>
    /// Gets the total size of all files matching a pattern.
    /// </summary>
    /// <param name="directoryPath">The directory to search.</param>
    /// <param name="searchPattern">The file search pattern.</param>
    /// <returns>The total size in bytes.</returns>
    public static long GetDirectorySize(string directoryPath, string searchPattern = "*.json")
    {
        if (!Directory.Exists(directoryPath))
            return 0;

        var files = Directory.GetFiles(directoryPath, searchPattern);
        return files.Sum(GetFileSize);
    }

    /// <summary>
    /// Gets all files matching a pattern, sorted by creation time (oldest first).
    /// </summary>
    /// <param name="directoryPath">The directory to search.</param>
    /// <param name="searchPattern">The file search pattern.</param>
    /// <returns>An ordered list of file paths.</returns>
    public static IEnumerable<string> GetFilesOrderedByAge(string directoryPath, string searchPattern = "*.json")
    {
        if (!Directory.Exists(directoryPath))
            return Enumerable.Empty<string>();

        return Directory.GetFiles(directoryPath, searchPattern)
            .Select(f => new FileInfo(f))
            .OrderBy(fi => fi.CreationTime)
            .Select(fi => fi.FullName);
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    /// <param name="directoryPath">The directory path.</param>
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }

    /// <summary>
    /// Safely deletes a file, ignoring errors if it doesn't exist.
    /// </summary>
    /// <param name="filePath">The file path to delete.</param>
    public static void SafeDelete(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Ignore deletion errors
        }
    }

    /// <summary>
    /// Formats a byte count as a human-readable string.
    /// </summary>
    /// <param name="bytes">The number of bytes.</param>
    /// <returns>A formatted string like "1.5 MB".</returns>
    public static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        var counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
    }
}
