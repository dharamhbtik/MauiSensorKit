using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MauiSensorKit;

/// <summary>
/// Core implementation of the background sensor recording and batching service.
/// </summary>
public sealed class SensorRecordingService : ISensorRecordingService, IDisposable
{
    private readonly ISensorCollectionService _sensorService;
    private readonly ILocalStorageService _localStorage;
    private readonly SensorRecordingOptions _options;
    private readonly ILogger<SensorRecordingService> _logger;
    private readonly ConcurrentQueue<SensorReading> _buffer = new();
    
    private CancellationTokenSource? _cts;
    private Task? _recordingTask;
    private bool _isRecording;
    private bool _disposed;

    /// <inheritdoc/>
    public bool IsRecording => _isRecording;

    public SensorRecordingService(
        ISensorCollectionService sensorService,
        ILocalStorageService localStorage,
        IOptions<SensorRecordingOptions> options,
        ILogger<SensorRecordingService> logger)
    {
        _sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public async Task StartRecordingAsync(CancellationToken cancellationToken = default)
    {
        if (_isRecording) return;
        _isRecording = true;

        _logger.LogInformation("Starting automated sensor recording");
        
        // 1. Start the actual sensors through the collection service
        await _sensorService.StartAsync(cancellationToken);
        _sensorService.ReadingRecorded += OnSensorReadingRecorded;

        // 2. Start the background batching loop
        _cts = new CancellationTokenSource();
        _recordingTask = BackgroundBatchingLoopAsync(_cts.Token);
    }

    /// <inheritdoc/>
    public async Task StopRecordingAsync()
    {
        if (!_isRecording) return;
        _isRecording = false;

        _logger.LogInformation("Stopping automated sensor recording");

        _sensorService.ReadingRecorded -= OnSensorReadingRecorded;
        await _sensorService.StopAsync();

        if (_cts != null)
        {
            _cts.Cancel();
            if (_recordingTask != null)
            {
                try
                {
                    await _recordingTask;
                }
                catch (OperationCanceledException) { }
            }
            _cts.Dispose();
            _cts = null;
        }

        // Flush any remaining items in the buffer
        await FlushBufferAsync(CancellationToken.None);
    }

    /// <inheritdoc/>
    public Task<string> ExportRecordingsToZipAsync(CancellationToken cancellationToken = default)
    {
        return _localStorage.ExportToZipAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<string> ExportRecordingsToTextAsync(CancellationToken cancellationToken = default)
    {
        return _localStorage.ExportToTextFileAsync(cancellationToken);
    }

    private void OnSensorReadingRecorded(object? sender, SensorReading reading)
    {
        // Only buffer the sensors the user explicitly wants to record
        if (_options.SensorsToRecord.Contains(reading.Type))
        {
            _buffer.Enqueue(reading);
        }
    }

    private async Task BackgroundBatchingLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_options.BatchInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                await FlushBufferAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in automated sensor recording batching loop");
        }
    }

    private async Task FlushBufferAsync(CancellationToken cancellationToken)
    {
        if (_buffer.IsEmpty) return;

        var readings = new List<SensorReading>();
        while (_buffer.TryDequeue(out var reading))
        {
            readings.Add(reading);
        }

        if (readings.Count == 0) return;

        var sessionId = _sensorService.CurrentSessionId ?? Guid.NewGuid().ToString("N");

        var batch = new SensorDataBatch
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            DeviceId = Microsoft.Maui.Devices.DeviceInfo.Current.Idiom.ToString() + "-" + Microsoft.Maui.Devices.DeviceInfo.Current.Platform.ToString(),
            BatchCreatedAt = DateTimeOffset.UtcNow,
            Readings = readings
        };

        try
        {
            await _localStorage.SaveBatchAsync(batch, cancellationToken);
            _logger.LogDebug("Flushed {Count} sensor readings to local batch {BatchId}", readings.Count, batch.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save sensor batch to local storage");
            // Optionally, put them back if we want to try again
            // foreach (var r in readings) _buffer.Enqueue(r);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _sensorService.ReadingRecorded -= OnSensorReadingRecorded;
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
