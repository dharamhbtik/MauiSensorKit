using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace MauiSensorKit;

/// <summary>
/// Service that coordinates sensor data collection from all enabled sensors.
/// </summary>
public sealed class SensorCollectionService : ISensorCollectionService, IDisposable, IAsyncDisposable
{
    private readonly IEnumerable<BaseSensorCollector> _collectors;
    private readonly ILocalStorageService _localStorage;
    private readonly SensorKitOptions _options;
    private readonly SensorAvailabilityChecker _availabilityChecker;
    private readonly ILogger<SensorCollectionService> _logger;

    private readonly ConcurrentQueue<SensorReading> _readingQueue = new();
    private CancellationTokenSource? _flushCts;
    private Task? _flushTask;
    private readonly Dictionary<SensorType, int> _readingCounts = new();
    private readonly object _lock = new();

    /// <inheritdoc/>
    public bool IsRunning { get; private set; }

    /// <inheritdoc/>
    public string? CurrentSessionId { get; private set; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<SensorType, SensorAvailabilityStatus>? LastAvailabilityReport { get; private set; }

    /// <inheritdoc/>
    public event EventHandler<SensorReading>? ReadingRecorded;

    /// <inheritdoc/>
    public event EventHandler<SensorAvailabilityReport>? AvailabilityChecked;

    /// <summary>
    /// Initializes a new instance of the <see cref="SensorCollectionService"/> class.
    /// </summary>
    /// <param name="collectors">The collection of sensor collectors.</param>
    /// <param name="localStorage">The local storage service.</param>
    /// <param name="options">The sensor kit options.</param>
    /// <param name="availabilityChecker">The sensor availability checker.</param>
    /// <param name="logger">The logger instance.</param>
    public SensorCollectionService(
        IEnumerable<BaseSensorCollector> collectors,
        ILocalStorageService localStorage,
        IOptions<SensorKitOptions> options,
        SensorAvailabilityChecker availabilityChecker,
        ILogger<SensorCollectionService> logger)
    {
        _collectors = collectors ?? throw new ArgumentNullException(nameof(collectors));
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _availabilityChecker = availabilityChecker ?? throw new ArgumentNullException(nameof(availabilityChecker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize reading counts
        foreach (var sensor in Enum.GetValues<SensorType>())
        {
            _readingCounts[sensor] = 0;
        }
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            _logger.LogWarning("Sensor collection is already running");
            return;
        }

        // Validate options
        var errors = _options.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Invalid configuration: {string.Join(", ", errors)}");
        }

        // Generate new session ID
        CurrentSessionId = Guid.NewGuid().ToString("N");
        _logger.LogInformation("Starting sensor collection session: {SessionId}", CurrentSessionId);

        // Check sensor availability
        var availability = await _availabilityChecker.CheckAllAsync(cancellationToken);
        LastAvailabilityReport = availability.Statuses.AsReadOnly();
        AvailabilityChecked?.Invoke(this, availability);

        _logger.LogInformation(
            "Sensor availability: {Available} available, {Permission} need permission, {Unavailable} unavailable",
            availability.AvailableCount,
            availability.PermissionNeededCount,
            availability.UnavailableCount);

        // Start enabled and available collectors
        var startedCount = 0;
        foreach (var collector in _collectors)
        {
            try
            {
                var sensorType = collector.SensorType;
                var status = availability.GetStatus(sensorType);

                // Skip if not enabled
                if (!_options.IsEnabled(sensorType))
                {
                    _logger.LogDebug("Skipping {Sensor} - not enabled in options", sensorType);
                    continue;
                }

                // Skip if not supported or unavailable
                if (!status.IsPotentiallyUsable())
                {
                    _logger.LogDebug("Skipping {Sensor} - unavailable ({Status})", sensorType, status);
                    continue;
                }

                // Check if actually supported
                var isSupported = await collector.IsSupportedAsync();
                if (!isSupported)
                {
                    _logger.LogDebug("Skipping {Sensor} - IsSupported returned false", sensorType);
                    continue;
                }

                // Subscribe to reading events
                collector.ReadingAvailable += OnCollectorReadingAvailable;

                // Start the collector
                await collector.StartAsync(CurrentSessionId, cancellationToken);
                startedCount++;

                _logger.LogInformation("Started {Sensor} collector", sensorType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting {Sensor} collector", collector.SensorType);
                // Continue with other collectors - don't let one failure stop everything
            }
        }

        _logger.LogInformation("Started {Count} sensor collectors", startedCount);

        // Start flush loop
        _flushCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _flushTask = RunFlushLoopAsync(_flushCts.Token);

        IsRunning = true;
    }

    /// <inheritdoc/>
    public async Task StopAsync()
    {
        if (!IsRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping sensor collection session: {SessionId}", CurrentSessionId);

        // Stop flush loop
        _flushCts?.Cancel();
        if (_flushTask != null)
        {
            try
            {
                await _flushTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        // Final flush
        await FlushAsync();

        // Stop all collectors
        foreach (var collector in _collectors.Where(c => c.IsRunning))
        {
            try
            {
                collector.ReadingAvailable -= OnCollectorReadingAvailable;
                await collector.StopAsync();
                _logger.LogInformation("Stopped {Sensor} collector", collector.SensorType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping {Sensor} collector", collector.SensorType);
            }
        }

        IsRunning = false;
        CurrentSessionId = null;
        _flushCts = null;
        _flushTask = null;

        _logger.LogInformation("Sensor collection stopped");
    }

    private void OnCollectorReadingAvailable(object? sender, SensorReading reading)
    {
        EnqueueReading(reading);
    }

    internal void EnqueueReading(SensorReading reading)
    {
        _readingQueue.Enqueue(reading);

        // Update count
        lock (_lock)
        {
            _readingCounts[reading.Type] = _readingCounts.GetValueOrDefault(reading.Type) + 1;
        }

        // Raise event
        try
        {
            ReadingRecorded?.Invoke(this, reading);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error raising ReadingRecorded event");
        }

        // Check if we should flush immediately
        if (_readingQueue.Count >= _options.BatchSize)
        {
            _ = Task.Run(async () => await FlushAsync());
        }
    }

    private async Task RunFlushLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_options.BatchFlushInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            try
            {
                if (!_readingQueue.IsEmpty)
                {
                    await FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in flush loop");
            }
        }
    }

    internal async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_readingQueue.IsEmpty)
        {
            return;
        }

        // Dequeue readings up to batch size
        var readings = new List<SensorReading>();
        while (readings.Count < _options.BatchSize && _readingQueue.TryDequeue(out var reading))
        {
            readings.Add(reading);
        }

        if (readings.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Flushing {Count} readings to storage", readings.Count);

        // Create batch
        var batch = new SensorDataBatch
        {
            SessionId = CurrentSessionId ?? "unknown",
            DeviceId = readings.First().DeviceId,
            Readings = readings,
            BatchCreatedAt = DateTimeOffset.UtcNow
        };

        // Save to local storage
        if (_options.EnableLocalStorage)
        {
            try
            {
                await _localStorage.SaveBatchAsync(batch, cancellationToken);
                _logger.LogDebug("Saved batch {BatchId} with {Count} readings", batch.Id, readings.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving batch to local storage");
            }
        }
    }

    /// <summary>
    /// Gets the reading count for a specific sensor type.
    /// </summary>
    /// <param name="sensorType">The sensor type.</param>
    /// <returns>The number of readings recorded for this sensor.</returns>
    public int GetReadingCount(SensorType sensorType)
    {
        lock (_lock)
        {
            return _readingCounts.GetValueOrDefault(sensorType);
        }
    }

    /// <summary>
    /// Gets all reading counts.
    /// </summary>
    /// <returns>A dictionary of sensor types to reading counts.</returns>
    public IReadOnlyDictionary<SensorType, int> GetAllReadingCounts()
    {
        lock (_lock)
        {
            return new Dictionary<SensorType, int>(_readingCounts);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (IsRunning)
        {
            StopAsync().GetAwaiter().GetResult();
        }

        _flushCts?.Dispose();
        _flushTask?.Dispose();
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (IsRunning)
        {
            await StopAsync();
        }

        _flushCts?.Dispose();
        if (_flushTask != null)
        {
            await _flushTask;
        }
    }
}
