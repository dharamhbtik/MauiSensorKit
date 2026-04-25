using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MauiSensorKit;

namespace MauiSensorKit.SampleApp.ViewModels;

/// <summary>
/// Represents a live sensor reading item in the dashboard.
/// </summary>
public partial class SensorLiveReadingItem : ObservableObject
{
    /// <summary>
    /// Gets the sensor type.
    /// </summary>
    public required SensorType Type { get; init; }

    /// <summary>
    /// Gets the sensor name.
    /// </summary>
    public string Name => Type.ToString();

    /// <summary>
    /// Gets the icon.
    /// </summary>
    public string Icon => Type.GetIconName();

    /// <summary>
    /// Gets or sets the formatted value.
    /// </summary>
    [ObservableProperty]
    private string _formattedValue = "No data";

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    [ObservableProperty]
    private DateTimeOffset _lastUpdated;

    /// <summary>
    /// Gets or sets the reading count.
    /// </summary>
    [ObservableProperty]
    private int _readingCount;

    /// <summary>
    /// Updates the display from a sensor reading.
    /// </summary>
    /// <param name="reading">The sensor reading.</param>
    public void UpdateFromReading(SensorReading reading)
    {
        FormattedValue = reading.ToString() ?? "No data";
        LastUpdated = reading.Timestamp;
        ReadingCount++;
    }
}

/// <summary>
/// ViewModel for the dashboard page.
/// </summary>
public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly ISensorCollectionService _sensorService;
    private readonly ILocalStorageService _localStorage;
    private readonly IUploadService _uploadService;
    private readonly ILogger<DashboardViewModel> _logger;

    private Stopwatch? _stopwatch;
    private CancellationTokenSource? _updateCts;

    /// <summary>
    /// Gets the collection of live sensor readings.
    /// </summary>
    public ObservableCollection<SensorLiveReadingItem> LiveReadings { get; } = new();

    /// <summary>
    /// Gets whether recording is active.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanStart))]
    [NotifyPropertyChangedFor(nameof(CanStop))]
    private bool _isRecording;

    /// <summary>
    /// Gets the current session ID.
    /// </summary>
    [ObservableProperty]
    private string? _sessionId;

    /// <summary>
    /// Gets the elapsed recording time.
    /// </summary>
    [ObservableProperty]
    private TimeSpan _elapsed;

    /// <summary>
    /// Gets the formatted storage size.
    /// </summary>
    [ObservableProperty]
    private string _storageSizeFormatted = "0 B";

    /// <summary>
    /// Gets the pending batch count.
    /// </summary>
    [ObservableProperty]
    private int _pendingBatchCount;

    /// <summary>
    /// Gets whether recording can be started.
    /// </summary>
    public bool CanStart => !IsRecording;

    /// <summary>
    /// Gets whether recording can be stopped.
    /// </summary>
    public bool CanStop => IsRecording;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardViewModel"/> class.
    /// </summary>
    /// <param name="sensorService">The sensor collection service.</param>
    /// <param name="localStorage">The local storage service.</param>
    /// <param name="uploadService">The upload service.</param>
    /// <param name="logger">The logger instance.</param>
    public DashboardViewModel(
        ISensorCollectionService sensorService,
        ILocalStorageService localStorage,
        IUploadService uploadService,
        ILogger<DashboardViewModel> logger)
    {
        _sensorService = sensorService ?? throw new ArgumentNullException(nameof(sensorService));
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _uploadService = uploadService ?? throw new ArgumentNullException(nameof(uploadService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        StartCommand = new AsyncRelayCommand(StartAsync, () => CanStart);
        StopCommand = new AsyncRelayCommand(StopAsync, () => CanStop);
        ForceUploadCommand = new AsyncRelayCommand(ForceUploadAsync);

        // Subscribe to sensor readings
        _sensorService.ReadingRecorded += OnReadingRecorded;

        // Initialize readings collection
        InitializeReadings();

        // Start update loop
        _updateCts = new CancellationTokenSource();
        _ = UpdateLoopAsync(_updateCts.Token);
    }

    private void InitializeReadings()
    {
        LiveReadings.Clear();

        // Load enabled sensors
        var enabledJson = Preferences.Default.Get<string?>("MauiSensorKit_EnabledSensors", null);
        if (!string.IsNullOrEmpty(enabledJson))
        {
            try
            {
                var enabled = System.Text.Json.JsonSerializer.Deserialize<Dictionary<SensorType, bool>>(enabledJson);
                if (enabled != null)
                {
                    foreach (var (sensor, isEnabled) in enabled)
                    {
                        if (isEnabled && !sensor.IsHardwareGated())
                        {
                            LiveReadings.Add(new SensorLiveReadingItem
                            {
                                Type = sensor
                            });
                        }
                    }
                }
            }
            catch { }
        }
    }

    private void OnReadingRecorded(object? sender, SensorReading reading)
    {
        var item = LiveReadings.FirstOrDefault(r => r.Type == reading.Type);
        if (item != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                item.UpdateFromReading(reading);
            });
        }
    }

    private async Task UpdateLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Update elapsed time
                if (IsRecording && _stopwatch != null)
                {
                    Elapsed = _stopwatch.Elapsed;
                }

                // Update storage info
                var storageSize = await _localStorage.GetStorageSizeInBytesAsync(cancellationToken);
                StorageSizeFormatted = FileHelper.FormatBytes(storageSize);

                PendingBatchCount = await _localStorage.GetPendingBatchCountAsync(cancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in update loop");
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
        }
    }

    /// <summary>
    /// Command to start recording.
    /// </summary>
    public IAsyncRelayCommand StartCommand { get; }

    /// <summary>
    /// Command to stop recording.
    /// </summary>
    public IAsyncRelayCommand StopCommand { get; }

    /// <summary>
    /// Command to force upload.
    /// </summary>
    public IAsyncRelayCommand ForceUploadCommand { get; }

    private async Task StartAsync()
    {
        try
        {
            // Start foreground service for background recording
#if ANDROID
            var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? global::Android.App.Application.Context;
            Platforms.Android.Services.SensorRecordingService.StartService(context);
#endif

            await _sensorService.StartAsync();

            IsRecording = true;
            SessionId = _sensorService.CurrentSessionId;
            _stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Started recording session {SessionId}", SessionId);

            StartCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting recording");
        }
    }

    private async Task StopAsync()
    {
        try
        {
            await _sensorService.StopAsync();

            // Stop foreground service
#if ANDROID
            var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? global::Android.App.Application.Context;
            Platforms.Android.Services.SensorRecordingService.StopService(context);
#endif

            IsRecording = false;
            _stopwatch?.Stop();
            Elapsed = _stopwatch?.Elapsed ?? TimeSpan.Zero;

            _logger.LogInformation("Stopped recording session");

            StartCommand.NotifyCanExecuteChanged();
            StopCommand.NotifyCanExecuteChanged();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping recording");
        }
    }

    private async Task ForceUploadAsync()
    {
        try
        {
            var uploaded = await _uploadService.ProcessPendingUploadsAsync();
            _logger.LogInformation("Force upload completed: {Count} batches uploaded", uploaded);

            // Refresh storage info
            var storageSize = await _localStorage.GetStorageSizeInBytesAsync();
            StorageSizeFormatted = FileHelper.FormatBytes(storageSize);
            PendingBatchCount = await _localStorage.GetPendingBatchCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during force upload");
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _sensorService.ReadingRecorded -= OnReadingRecorded;
        _updateCts?.Cancel();
        _updateCts?.Dispose();
    }
}
