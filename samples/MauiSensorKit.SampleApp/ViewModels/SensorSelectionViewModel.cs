using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
                }
            }

            UpdateGroupedSensors();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading sensor availability");
        }
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
