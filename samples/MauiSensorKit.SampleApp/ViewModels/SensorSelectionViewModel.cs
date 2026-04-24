using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using MauiSensorKit;

namespace MauiSensorKit.SampleApp.ViewModels;

/// <summary>
/// Represents a sensor toggle item in the UI.
/// </summary>
public partial class SensorToggleItem : ObservableObject
{
    /// <summary>
    /// Gets the sensor type.
    /// </summary>
    public SensorType Type { get; }

    /// <summary>
    /// Gets the human-readable name.
    /// </summary>
    public string Name => Type.ToString();

    /// <summary>
    /// Gets the description.
    /// </summary>
    public string Description => Type.GetDescription();

    /// <summary>
    /// Gets the icon name.
    /// </summary>
    public string Icon => Type.GetIconName();

    /// <summary>
    /// Gets the category.
    /// </summary>
    public string Category => Type.GetCategory();

    /// <summary>
    /// Gets or sets whether the sensor is enabled.
    /// </summary>
    [ObservableProperty]
    private bool _isEnabled;

    /// <summary>
    /// Gets the availability status.
    /// </summary>
    [ObservableProperty]
    private SensorAvailabilityStatus _availability;

    /// <summary>
    /// Gets whether the sensor is available (or needs permission).
    /// </summary>
    public bool IsAvailable => Availability == SensorAvailabilityStatus.Available || Availability == SensorAvailabilityStatus.PermissionNeeded;

    /// <summary>
    /// Gets whether the sensor is not supported.
    /// </summary>
    public bool IsNotSupported => Availability == SensorAvailabilityStatus.NotSupported;

    /// <summary>
    /// Gets whether the sensor needs permission.
    /// </summary>
    public bool NeedsPermission => Availability == SensorAvailabilityStatus.PermissionNeeded;

    /// <summary>
    /// Gets whether the sensor can be toggled (available or needs permission).
    /// </summary>
    public bool CanBeToggled => Availability != SensorAvailabilityStatus.NotSupported && Availability != SensorAvailabilityStatus.Unavailable;

    /// <summary>
    /// Gets the permission status label.
    /// </summary>
    public string PermissionLabel => NeedsPermission ? "⚠️ Permission Required" : string.Empty;

    /// <summary>
    /// Gets the required permissions for this sensor.
    /// </summary>
    public string RequiredPermissions => GetRequiredPermissions(Type);

    /// <summary>
    /// Gets the availability label.
    /// </summary>
    public string AvailabilityLabel => Availability.GetLabel();

    /// <summary>
    /// Gets the availability color.
    /// </summary>
    public Color AvailabilityColor => Availability.GetColor();

    /// <summary>
    /// Initializes a new instance of the <see cref="SensorToggleItem"/> class.
    /// </summary>
    /// <param name="type">The sensor type.</param>
    public SensorToggleItem(SensorType type)
    {
        Type = type;
    }

    private static string GetRequiredPermissions(SensorType type)
    {
        return type switch
        {
            SensorType.Location => "Location (Fine/Coarse)",
            SensorType.Microphone => "Microphone",
            SensorType.Nfc => "NFC",
            SensorType.StepCounter or SensorType.StepDetector => "Activity Recognition",
            _ => "None"
        };
    }
}

/// <summary>
/// ViewModel for the sensor selection page.
/// </summary>
public partial class SensorSelectionViewModel : ObservableObject
{
    private readonly SensorAvailabilityChecker _availabilityChecker;
    private readonly ILogger<SensorSelectionViewModel> _logger;

    /// <summary>
    /// Gets the collection of sensor toggle items.
    /// </summary>
    public ObservableCollection<SensorToggleItem> Sensors { get; } = new();

    /// <summary>
    /// Gets the grouped sensors.
    /// </summary>
    public ObservableCollection<SensorGroup> GroupedSensors { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="SensorSelectionViewModel"/> class.
    /// </summary>
    /// <param name="availabilityChecker">The sensor availability checker.</param>
    /// <param name="logger">The logger instance.</param>
    public SensorSelectionViewModel(SensorAvailabilityChecker availabilityChecker, ILogger<SensorSelectionViewModel> logger)
    {
        _availabilityChecker = availabilityChecker ?? throw new ArgumentNullException(nameof(availabilityChecker));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadAvailabilityCommand = new AsyncRelayCommand(LoadAvailabilityAsync);
        SaveSelectionCommand = new AsyncRelayCommand(SaveSelectionAsync);
        EnableAllCommand = new RelayCommand(EnableAll);
        DisableAllCommand = new RelayCommand(DisableAll);
        AutoSelectAvailableCommand = new AsyncRelayCommand(AutoSelectAvailableAsync);
        RequestPermissionCommand = new AsyncRelayCommand<SensorToggleItem>(RequestPermissionAsync);

        // Initialize with all sensors
        InitializeSensors();
    }

    private void InitializeSensors()
    {
        Sensors.Clear();

        // Load saved preferences
        var enabledJson = Preferences.Default.Get<string?>("MauiSensorKit_EnabledSensors", null);
        var enabledSensors = new Dictionary<SensorType, bool>();
        if (!string.IsNullOrEmpty(enabledJson))
        {
            try
            {
                enabledSensors = System.Text.Json.JsonSerializer.Deserialize<Dictionary<SensorType, bool>>(enabledJson) ?? new();
            }
            catch { }
        }

        foreach (var sensorType in Enum.GetValues<SensorType>())
        {
            var item = new SensorToggleItem(sensorType)
            {
                IsEnabled = enabledSensors.GetValueOrDefault(sensorType, !sensorType.IsHardwareGated()),
                Availability = SensorAvailabilityStatus.Unknown
            };
            Sensors.Add(item);
        }

        UpdateGroupedSensors();
    }

    private void UpdateGroupedSensors()
    {
        GroupedSensors.Clear();

        var groups = Sensors
            .GroupBy(s => s.Category)
            .OrderBy(g => GetCategoryOrder(g.Key))
            .Select(g => new SensorGroup(g.Key, g.ToList()));

        foreach (var group in groups)
        {
            GroupedSensors.Add(group);
        }
    }

    private static int GetCategoryOrder(string category) => category switch
    {
        "Motion Sensors" => 0,
        "Environment Sensors" => 1,
        "Location & Connectivity" => 2,
        "Device" => 3,
        "Security & Identity (Not Supported)" => 4,
        _ => 5
    };

    /// <summary>
    /// Command to load sensor availability.
    /// </summary>
    public IAsyncRelayCommand LoadAvailabilityCommand { get; }

    /// <summary>
    /// Command to save sensor selection.
    /// </summary>
    public IAsyncRelayCommand SaveSelectionCommand { get; }

    /// <summary>
    /// Command to enable all sensors.
    /// </summary>
    public IRelayCommand EnableAllCommand { get; }

    /// <summary>
    /// Command to disable all sensors.
    /// </summary>
    public IRelayCommand DisableAllCommand { get; }

    /// <summary>
    /// Command to auto select all available sensors.
    /// </summary>
    public IAsyncRelayCommand AutoSelectAvailableCommand { get; }

    /// <summary>
    /// Command to request permission for a sensor.
    /// </summary>
    public IAsyncRelayCommand<SensorToggleItem> RequestPermissionCommand { get; }

    /// <summary>
    /// Gets a value indicating whether any sensors need permission.
    /// </summary>
    public bool HasSensorsNeedingPermission => Sensors.Any(s => s.NeedsPermission);

    /// <summary>
    /// Gets the count of available sensors.
    /// </summary>
    public int AvailableCount => Sensors.Count(s => s.Availability == SensorAvailabilityStatus.Available);

    /// <summary>
    /// Gets the count of sensors needing permission.
    /// </summary>
    public int PermissionNeededCount => Sensors.Count(s => s.NeedsPermission);

    /// <summary>
    /// Gets the status summary text.
    /// </summary>
    public string StatusSummary => $"Available: {AvailableCount} | Need Permission: {PermissionNeededCount} | Unavailable: {Sensors.Count(s => s.Availability == SensorAvailabilityStatus.Unavailable)}";

    private async Task LoadAvailabilityAsync()
    {
        try
        {
            var report = await _availabilityChecker.CheckAllAsync();

            foreach (var sensor in Sensors)
            {
                if (report.Statuses.TryGetValue(sensor.Type, out var status))
                {
                    sensor.Availability = status;
                    // Auto-select available sensors by default
                    if (status == SensorAvailabilityStatus.Available && !sensor.IsEnabled)
                    {
                        sensor.IsEnabled = true;
                    }
                }
            }

            UpdateGroupedSensors();
            OnPropertyChanged(nameof(HasSensorsNeedingPermission));
            OnPropertyChanged(nameof(AvailableCount));
            OnPropertyChanged(nameof(PermissionNeededCount));
            OnPropertyChanged(nameof(StatusSummary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sensor availability");
        }
    }

    private async Task AutoSelectAvailableAsync()
    {
        try
        {
            foreach (var sensor in Sensors.Where(s => s.Availability == SensorAvailabilityStatus.Available))
            {
                sensor.IsEnabled = true;
            }

            _logger.LogInformation("Auto-selected all available sensors");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error auto-selecting sensors");
        }
    }

    private async Task RequestPermissionAsync(SensorToggleItem? item)
    {
        if (item == null) return;

        try
        {
            var permissionStatus = await RequestSensorPermissionAsync(item.Type);

            if (permissionStatus == PermissionStatus.Granted)
            {
                // Re-check availability
                var newStatus = await _availabilityChecker.CheckSensorAsync(item.Type);
                item.Availability = newStatus;

                if (newStatus == SensorAvailabilityStatus.Available)
                {
                    item.IsEnabled = true;
                    await Shell.Current.DisplayAlert("Permission Granted", $"Permission granted for {item.Name}. Sensor is now available.", "OK");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Permission Check", $"Permission may have been granted, but {item.Name} is still not available.", "OK");
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Permission Denied", $"Permission was not granted for {item.Name}. Please enable it in device settings.", "OK");
            }

            OnPropertyChanged(nameof(HasSensorsNeedingPermission));
            OnPropertyChanged(nameof(AvailableCount));
            OnPropertyChanged(nameof(PermissionNeededCount));
            OnPropertyChanged(nameof(StatusSummary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting permission for {Sensor}", item.Type);
            await Shell.Current.DisplayAlert("Error", $"Failed to request permission: {ex.Message}", "OK");
        }
    }

    private static async Task<PermissionStatus> RequestSensorPermissionAsync(SensorType sensorType)
    {
        return sensorType switch
        {
            SensorType.Location => await Permissions.RequestAsync<Permissions.LocationWhenInUse>(),
            SensorType.Microphone => await Permissions.RequestAsync<Permissions.Microphone>(),
            SensorType.StepCounter or SensorType.StepDetector => await CheckActivityPermissionAsync(),
            _ => PermissionStatus.Unknown
        };
    }

    private static async Task<PermissionStatus> CheckActivityPermissionAsync()
    {
        // Activity recognition permission varies by platform
#if ANDROID
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Sensors>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Sensors>();
            }
            return status;
        }
        catch
        {
            return PermissionStatus.Unknown;
        }
#else
        return PermissionStatus.Granted; // iOS handles this automatically
#endif
    }

    private async Task SaveSelectionAsync()
    {
        try
        {
            var enabledSensors = Sensors.ToDictionary(s => s.Type, s => s.IsEnabled);
            var json = System.Text.Json.JsonSerializer.Serialize(enabledSensors);
            Preferences.Default.Set("MauiSensorKit_EnabledSensors", json);

            await Shell.Current.GoToAsync("///dashboard");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving sensor selection");
        }
    }

    private void EnableAll()
    {
        foreach (var sensor in Sensors.Where(s => s.IsAvailable))
        {
            sensor.IsEnabled = true;
        }
    }

    private void DisableAll()
    {
        foreach (var sensor in Sensors.Where(s => s.IsAvailable))
        {
            sensor.IsEnabled = false;
        }
    }
}

/// <summary>
/// Represents a group of sensors in the UI.
/// </summary>
public class SensorGroup : List<SensorToggleItem>
{
    /// <summary>
    /// Gets the name of the group.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SensorGroup"/> class.
    /// </summary>
    /// <param name="name">The group name.</param>
    /// <param name="sensors">The sensors in the group.</param>
    public SensorGroup(string name, IEnumerable<SensorToggleItem> sensors) : base(sensors)
    {
        Name = name;
    }
}
