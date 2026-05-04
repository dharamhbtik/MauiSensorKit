namespace MauiSensorKit;

/// <summary>
/// Defines the target platform for sensor data uploads.
/// </summary>
public enum UploadTarget
{
    /// <summary>
    /// Uploads to a custom API endpoint via HTTP POST.
    /// </summary>
    CustomApi,

    /// <summary>
    /// Uploads to a Firebase Realtime Database.
    /// </summary>
    Firebase
}

/// <summary>
/// Configuration options for MauiSensorKit data upload functionality.
/// </summary>
public class SensorKitUploadOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether automatic upload is enabled.
    /// </summary>
    public bool EnableUpload { get; set; }

    /// <summary>
    /// Gets or sets the target platform for uploads. Defaults to CustomApi.
    /// </summary>
    public UploadTarget Target { get; set; } = UploadTarget.CustomApi;

    /// <summary>
    /// Gets or sets the custom API endpoint URL for uploading sensor data. Required if Target is CustomApi.
    /// </summary>
    public string? ApiEndpointUrl { get; set; }

    /// <summary>
    /// Gets or sets the Firebase Realtime Database URL. Required if Target is Firebase.
    /// Example: https://your-project-id.firebaseio.com
    /// </summary>
    public string? FirebaseDatabaseUrl { get; set; }

    /// <summary>
    /// Gets or sets an optional authentication token for Firebase requests.
    /// </summary>
    public string? FirebaseAuthToken { get; set; }

    /// <summary>
    /// Gets or sets the custom headers to include in upload requests.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();

    /// <summary>
    /// Gets or sets the interval between upload retry attempts.
    /// </summary>
    public TimeSpan UploadRetryInterval { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the maximum number of retry attempts for failed uploads.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets a value indicating whether uploads should only occur on WiFi.
    /// </summary>
    public bool UploadOnlyOnWifi { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to delete local files after successful upload.
    /// </summary>
    public bool DeleteAfterUpload { get; set; } = true;

    /// <summary>
    /// Gets or sets the timeout for upload HTTP requests.
    /// </summary>
    public TimeSpan UploadTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets a value indicating whether to compress upload data.
    /// </summary>
    public bool EnableCompression { get; set; } = true;

    /// <summary>
    /// Validates the upload options configuration.
    /// </summary>
    /// <returns>A list of validation errors, or empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (EnableUpload)
        {
            if (Target == UploadTarget.CustomApi)
            {
                if (string.IsNullOrWhiteSpace(ApiEndpointUrl))
                    errors.Add("ApiEndpointUrl is required when Target is CustomApi");
                else if (!Uri.TryCreate(ApiEndpointUrl, UriKind.Absolute, out _))
                    errors.Add("ApiEndpointUrl must be a valid absolute URL");
            }
            else if (Target == UploadTarget.Firebase)
            {
                if (string.IsNullOrWhiteSpace(FirebaseDatabaseUrl))
                    errors.Add("FirebaseDatabaseUrl is required when Target is Firebase");
                else if (!Uri.TryCreate(FirebaseDatabaseUrl, UriKind.Absolute, out _))
                    errors.Add("FirebaseDatabaseUrl must be a valid absolute URL");
            }

            if (MaxRetryAttempts < 0)
                errors.Add("MaxRetryAttempts must be non-negative");

            if (UploadRetryInterval < TimeSpan.FromSeconds(10))
                errors.Add("UploadRetryInterval must be at least 10 seconds");
        }

        return errors;
    }
}
