using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MauiSensorKit;

/// <summary>
/// Background service for periodic upload of sensor data.
/// </summary>
public sealed class UploadBackgroundService : BackgroundService
{
    private readonly IUploadService _uploadService;
    private readonly IOptions<SensorKitUploadOptions> _options;
    private readonly ILogger<UploadBackgroundService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadBackgroundService"/> class.
    /// </summary>
    /// <param name="uploadService">The upload service.</param>
    /// <param name="options">The upload options.</param>
    /// <param name="logger">The logger instance.</param>
    public UploadBackgroundService(
        IUploadService uploadService,
        IOptions<SensorKitUploadOptions> options,
        ILogger<UploadBackgroundService> logger)
    {
        _uploadService = uploadService ?? throw new ArgumentNullException(nameof(uploadService));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Value.EnableUpload)
        {
            _logger.LogInformation("Upload background service disabled");
            return;
        }

        var errors = _options.Value.Validate();
        if (errors.Count > 0)
        {
            _logger.LogError("Invalid upload configuration: {Errors}", string.Join(", ", errors));
            return;
        }

        _logger.LogInformation(
            "Upload background service started with interval {Interval}",
            _options.Value.UploadRetryInterval);

        using var timer = new PeriodicTimer(_options.Value.UploadRetryInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await _uploadService.ProcessPendingUploadsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in upload background service");
            }
        }

        _logger.LogInformation("Upload background service stopped");
    }
}

/// <summary>
/// Service for uploading sensor data to a remote API.
/// </summary>
public sealed class UploadService : IUploadService
{
    private readonly ILocalStorageService _localStorage;
    private readonly SensorKitUploadOptions _options;
    private readonly ConnectivityHelper _connectivity;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UploadService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UploadService"/> class.
    /// </summary>
    /// <param name="localStorage">The local storage service.</param>
    /// <param name="options">The upload options.</param>
    /// <param name="connectivity">The connectivity helper.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="logger">The logger instance.</param>
    public UploadService(
        ILocalStorageService localStorage,
        IOptions<SensorKitUploadOptions> options,
        ConnectivityHelper connectivity,
        HttpClient httpClient,
        ILogger<UploadService> logger)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _connectivity = connectivity ?? throw new ArgumentNullException(nameof(connectivity));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task<int> ProcessPendingUploadsAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.EnableUpload)
        {
            _logger.LogDebug("Upload is disabled");
            return 0;
        }

        // Check connectivity
        if (!_connectivity.CanUpload(_options.UploadOnlyOnWifi))
        {
            _logger.LogDebug("Cannot upload due to connectivity restrictions");
            return 0;
        }

        // Get pending batches
        var pendingBatches = await _localStorage.GetPendingBatchesAsync(cancellationToken);
        if (pendingBatches.Count == 0)
        {
            return 0;
        }

        _logger.LogInformation("Processing {Count} pending batch uploads", pendingBatches.Count);

        var uploadedCount = 0;

        foreach (var entry in pendingBatches)
        {
            try
            {
                // Load the batch
                var batch = await _localStorage.LoadBatchAsync(entry, cancellationToken);
                if (batch == null)
                {
                    _logger.LogWarning("Could not load batch {FileName}", entry.FileName);
                    continue;
                }

                // Upload the batch
                var success = await UploadBatchWithRetryAsync(batch, cancellationToken);

                if (success)
                {
                    uploadedCount++;

                    // Mark as uploaded
                    await _localStorage.MarkBatchAsUploadedAsync(entry.FileName, cancellationToken);

                    // Delete if configured
                    if (_options.DeleteAfterUpload)
                    {
                        await _localStorage.DeleteBatchAsync(entry.FileName, cancellationToken);
                        _logger.LogDebug("Deleted uploaded batch {FileName}", entry.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch {FileName}", entry.FileName);
            }
        }

        _logger.LogInformation("Uploaded {Count} batches", uploadedCount);
        return uploadedCount;
    }

    /// <inheritdoc/>
    public async Task<bool> UploadBatchAsync(SensorDataBatch batch, CancellationToken cancellationToken = default)
    {
        return await UploadBatchWithRetryAsync(batch, cancellationToken);
    }

    private async Task<bool> UploadBatchWithRetryAsync(SensorDataBatch batch, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < _options.MaxRetryAttempts; attempt++)
        {
            try
            {
                var success = await TryUploadBatchAsync(batch, cancellationToken);
                if (success)
                {
                    return true;
                }

                // Don't retry on client errors (4xx)
                _logger.LogWarning("Upload attempt {Attempt} failed for batch {BatchId}", attempt + 1, batch.Id);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                _logger.LogWarning(ex, "Network error on upload attempt {Attempt}", attempt + 1);

                // Exponential backoff
                if (attempt < _options.MaxRetryAttempts - 1)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    _logger.LogInformation("Retrying in {Delay}ms...", delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error uploading batch {BatchId}", batch.Id);
                return false;
            }
        }

        _logger.LogError("Failed to upload batch {BatchId} after {Attempts} attempts", batch.Id, _options.MaxRetryAttempts);
        return false;
    }

    private async Task<bool> TryUploadBatchAsync(SensorDataBatch batch, CancellationToken cancellationToken)
    {
        string requestUrl;

        if (_options.Target == UploadTarget.CustomApi)
        {
            if (string.IsNullOrEmpty(_options.ApiEndpointUrl))
            {
                _logger.LogError("API endpoint URL is not configured");
                return false;
            }
            requestUrl = _options.ApiEndpointUrl;
        }
        else // Firebase
        {
            if (string.IsNullOrEmpty(_options.FirebaseDatabaseUrl))
            {
                _logger.LogError("Firebase database URL is not configured");
                return false;
            }
            
            var baseUrl = _options.FirebaseDatabaseUrl.TrimEnd('/');
            requestUrl = $"{baseUrl}/sensor_batches/{batch.SessionId}_{batch.Id}.json";
            
            if (!string.IsNullOrEmpty(_options.FirebaseAuthToken))
            {
                requestUrl += $"?auth={_options.FirebaseAuthToken}";
            }
        }

        // Serialize batch
        string json = JsonSerializer.Serialize(batch, SensorDataJsonContext.Default.SensorDataBatch);
        HttpContent content;

        if (_options.EnableCompression)
        {
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                await gzipStream.WriteAsync(jsonBytes, cancellationToken);
            }
            
            var compressedBytes = memoryStream.ToArray();
            var byteArrayContent = new ByteArrayContent(compressedBytes);
            byteArrayContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            byteArrayContent.Headers.ContentEncoding.Add("gzip");
            
            content = byteArrayContent;
        }
        else
        {
            content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        // Add custom headers
        foreach (var header in _options.Headers)
        {
            content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        // Send request
        using var cts = new CancellationTokenSource(_options.UploadTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);

        HttpResponseMessage response;
        if (_options.Target == UploadTarget.Firebase)
        {
            // Firebase REST API uses PUT to set data at a specific path, or POST to append to a list
            // We'll use PUT since we're generating a specific ID
            response = await _httpClient.PutAsync(requestUrl, content, linkedCts.Token);
        }
        else
        {
            response = await _httpClient.PostAsync(requestUrl, content, linkedCts.Token);
        }

        // Check response
        if (response.IsSuccessStatusCode)
        {
            _logger.LogDebug("Successfully uploaded batch {BatchId}", batch.Id);
            return true;
        }

        // Client errors (4xx) should not be retried
        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Client error {StatusCode} uploading batch {BatchId}: {Error}",
                (int)response.StatusCode,
                batch.Id,
                errorBody);
            return false;
        }

        // Server errors (5xx) should be retried
        _logger.LogWarning(
            "Server error {StatusCode} uploading batch {BatchId}",
            (int)response.StatusCode,
            batch.Id);
        return false;
    }
}
