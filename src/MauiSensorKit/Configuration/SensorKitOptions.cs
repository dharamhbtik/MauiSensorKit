using Microsoft.Maui.Devices.Sensors;

namespace MauiSensorKit;

/// <summary>
/// Configuration options for MauiSensorKit sensor collection.
/// </summary>
public class SensorKitOptions
{
    /// <summary>
    /// Gets or sets the dictionary of enabled sensors keyed by SensorType.
    /// </summary>
    public Dictionary<SensorType, bool> EnabledSensors { get; set; } = new();

    /// <summary>
    /// Gets or sets the sensor speed for motion sensors (accelerometer, gyroscope, magnetometer).
    /// </summary>
    public SensorSpeed MotionSensorSpeed { get; set; } = SensorSpeed.UI;

    /// <summary>
    /// Gets or sets the interval between location updates.
    /// </summary>
    public TimeSpan LocationInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the interval between battery status polling.
    /// </summary>
    public TimeSpan BatteryPollingInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the interval between microphone amplitude sampling.
    /// </summary>
    public TimeSpan MicrophonePollingInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the interval between slow sensor polling (temperature, humidity, etc.).
    /// </summary>
    public TimeSpan SlowSensorPollingInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets a value indicating whether local storage is enabled.
    /// </summary>
    public bool EnableLocalStorage { get; set; } = true;

    /// <summary>
    /// Gets or sets the custom local storage path. If null, uses FileSystem.AppDataDirectory.
    /// </summary>
    public string? LocalStoragePath { get; set; }

    /// <summary>
    /// Gets or sets the file name prefix for stored data files.
    /// </summary>
    public string FileNamePrefix { get; set; } = "sensor_data";

    /// <summary>
    /// Gets or sets the maximum size of a local file in megabytes.
    /// </summary>
    public int MaxLocalFileSizeMB { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum number of local files to keep.
    /// </summary>
    public int MaxLocalFileCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the number of readings per batch before flushing to storage.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the interval between automatic batch flushes.
    /// </summary>
    public TimeSpan BatchFlushInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enables all MAUI-supported sensors.
    /// </summary>
    public void EnableAll()
    {
        foreach (var sensor in Enum.GetValues<SensorType>())
        {
            if (!sensor.IsHardwareGated())
            {
                EnabledSensors[sensor] = true;
            }
        }
    }

    /// <summary>
    /// Disables all sensors.
    /// </summary>
    public void DisableAll()
    {
        EnabledSensors.Clear();
    }

    /// <summary>
    /// Enables a specific sensor.
    /// </summary>
    /// <param name="sensor">The sensor type to enable.</param>
    public void Enable(SensorType sensor)
    {
        EnabledSensors[sensor] = true;
    }

    /// <summary>
    /// Disables a specific sensor.
    /// </summary>
    /// <param name="sensor">The sensor type to disable.</param>
    public void Disable(SensorType sensor)
    {
        EnabledSensors[sensor] = false;
    }

    /// <summary>
    /// Determines whether a sensor is enabled.
    /// </summary>
    /// <param name="sensor">The sensor type to check.</param>
    /// <returns>True if the sensor is enabled; otherwise, false.</returns>
    public bool IsEnabled(SensorType sensor)
    {
        return EnabledSensors.TryGetValue(sensor, out var enabled) && enabled;
    }

    /// <summary>
    /// Creates default options with all MAUI-supported sensors enabled.
    /// </summary>
    /// <returns>A new <see cref="SensorKitOptions"/> instance with default settings.</returns>
    public static SensorKitOptions CreateDefault()
    {
        var options = new SensorKitOptions();
        options.EnableAll();
        return options;
    }

    /// <summary>
    /// Validates the options configuration.
    /// </summary>
    /// <returns>A list of validation errors, or empty if valid.</returns>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (BatchSize < 1)
            errors.Add("BatchSize must be at least 1");

        if (MaxLocalFileSizeMB < 1)
            errors.Add("MaxLocalFileSizeMB must be at least 1");

        if (MaxLocalFileCount < 1)
            errors.Add("MaxLocalFileCount must be at least 1");

        if (BatchFlushInterval < TimeSpan.FromSeconds(1))
            errors.Add("BatchFlushInterval must be at least 1 second");

        return errors;
    }
}
