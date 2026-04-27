namespace MauiSensorKit;

/// <summary>
/// Configuration item for a sensor in the sensor selection UI.
/// </summary>
public class SensorConfigItem
{
    /// <summary>
    /// Sensor type.
    /// </summary>
    public SensorType Type { get; set; }
    
    /// <summary>
    /// Display name.
    /// </summary>
    public string Name => Type.ToString();
    
    /// <summary>
    /// Short description.
    /// </summary>
    public string ShortDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Detailed description.
    /// </summary>
    public string DetailedDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Category emoji.
    /// </summary>
    public string CategoryEmoji { get; set; } = "🔧";
    
    /// <summary>
    /// Sensor emoji.
    /// </summary>
    public string SensorEmoji => Type.GetIconName();
    
    /// <summary>
    /// Sensor category.
    /// </summary>
    public SensorCategory Category { get; set; }
    
    /// <summary>
    /// Whether the sensor is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }
    
    /// <summary>
    /// Whether the sensor is supported on this device.
    /// </summary>
    public bool IsSupported { get; set; } = true;
    
    /// <summary>
    /// Whether the sensor requires permission.
    /// </summary>
    public bool RequiresPermission { get; set; }
    
    /// <summary>
    /// Update frequency mode.
    /// </summary>
    public SensorFrequencyMode FrequencyMode { get; set; }
    
    /// <summary>
    /// Data unit (e.g., "m/s²", "µT", "hPa").
    /// </summary>
    public string DataUnit { get; set; } = string.Empty;
    
    /// <summary>
    /// Data description (e.g., "X, Y, Z axes").
    /// </summary>
    public string DataDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// Required permissions.
    /// </summary>
    public List<string> RequiredPermissions { get; set; } = new();
    
    /// <summary>
    /// Compatible activities.
    /// </summary>
    public List<string> CompatibleActivities { get; set; } = new();
    
    /// <summary>
    /// Power impact level (Low, Medium, High).
    /// </summary>
    public string PowerImpact { get; set; } = "Low";
    
    /// <summary>
    /// Power impact icon representation.
    /// </summary>
    public string PowerImpactIcon => PowerImpact switch
    {
        "High" => "🔋🔋🔋",
        "Medium" => "🔋🔋",
        _ => "🔋"
    };
    
    /// <summary>
    /// Availability status.
    /// </summary>
    public SensorAvailabilityStatus Availability { get; set; } = SensorAvailabilityStatus.Available;
}

/// <summary>
/// Sensor frequency mode.
/// </summary>
public enum SensorFrequencyMode
{
    /// <summary>
    /// Event-driven updates.
    /// </summary>
    EventDriven,
    
    /// <summary>
    /// Polled at intervals.
    /// </summary>
    Polled,
    
    /// <summary>
    /// Continuous streaming.
    /// </summary>
    Continuous
}

/// <summary>
/// Sensor category for grouping.
/// </summary>
public enum SensorCategory
{
    /// <summary>
    /// Motion sensors (accelerometer, gyroscope, etc.).
    /// </summary>
    Motion,
    
    /// <summary>
    /// Environmental sensors (barometer, temperature, etc.).
    /// </summary>
    Environment,
    
    /// <summary>
    /// Location sensors (GPS).
    /// </summary>
    Location,
    
    /// <summary>
    /// Connectivity sensors (NFC, UWB).
    /// </summary>
    Connectivity,
    
    /// <summary>
    /// Device state sensors (battery, display, etc.).
    /// </summary>
    Device,
    
    /// <summary>
    /// Not supported on this device.
    /// </summary>
    NotSupported
}

/// <summary>
/// Sensor configuration group for UI.
/// </summary>
public class SensorConfigGroup
{
    /// <summary>
    /// Category.
    /// </summary>
    public SensorCategory Category { get; set; }
    
    /// <summary>
    /// Display title.
    /// </summary>
    public string Title => Category switch
    {
        SensorCategory.Motion => "Motion Sensors",
        SensorCategory.Environment => "Environmental Sensors",
        SensorCategory.Location => "Location",
        SensorCategory.Connectivity => "Connectivity",
        SensorCategory.Device => "Device State",
        SensorCategory.NotSupported => "Not Supported",
        _ => "Other"
    };
    
    /// <summary>
    /// Category description.
    /// </summary>
    public string Description => Category switch
    {
        SensorCategory.Motion => "Accelerometer, gyroscope, and motion detection",
        SensorCategory.Environment => "Barometer, temperature, and environmental data",
        SensorCategory.Location => "GPS and location tracking",
        SensorCategory.Connectivity => "NFC, UWB, and wireless sensors",
        SensorCategory.Device => "Battery, display, and device state",
        SensorCategory.NotSupported => "Sensors not available on this device",
        _ => ""
    };
    
    /// <summary>
    /// Icon/emoji.
    /// </summary>
    public string Icon => Category switch
    {
        SensorCategory.Motion => "🏃",
        SensorCategory.Environment => "🌍",
        SensorCategory.Location => "📍",
        SensorCategory.Connectivity => "📡",
        SensorCategory.Device => "📱",
        SensorCategory.NotSupported => "🚫",
        _ => "🔧"
    };
    
    /// <summary>
    /// Category color.
    /// </summary>
    public string Color => Category switch
    {
        SensorCategory.Motion => "#6C63FF",
        SensorCategory.Environment => "#00C896",
        SensorCategory.Location => "#00E5FF",
        SensorCategory.Connectivity => "#FF8C42",
        SensorCategory.Device => "#FFB347",
        SensorCategory.NotSupported => "#3D3D5C",
        _ => "#6C63FF"
    };
    
    /// <summary>
    /// Sensors in this group.
    /// </summary>
    public List<SensorConfigItem> Sensors { get; set; } = new();
    
    /// <summary>
    /// Number of enabled sensors.
    /// </summary>
    public int EnabledCount => Sensors.Count(s => s.IsEnabled);
    
    /// <summary>
    /// Number of available sensors.
    /// </summary>
    public int AvailableCount => Sensors.Count(s => s.IsSupported);
    
    /// <summary>
    /// Whether all sensors are enabled.
    /// </summary>
    public bool IsAllEnabled => Sensors.Any() && Sensors.All(s => s.IsEnabled);
}
