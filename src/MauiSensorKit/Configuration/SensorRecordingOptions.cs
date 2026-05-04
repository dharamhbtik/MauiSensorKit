namespace MauiSensorKit;

/// <summary>
/// Configuration options for automated background sensor recording in the MauiSensorKit.
/// </summary>
public class SensorRecordingOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether automated recording should start automatically.
    /// Default is false.
    /// </summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of sensors that should be enabled and recorded.
    /// Default includes Accelerometer, Gyroscope, and Battery.
    /// </summary>
    public HashSet<SensorType> SensorsToRecord { get; set; } = new()
    {
        SensorType.Accelerometer,
        SensorType.Gyroscope,
        SensorType.Battery
    };

    /// <summary>
    /// Gets or sets the interval at which memory buffers are flushed to a local JSON file batch.
    /// Default is 60 seconds.
    /// </summary>
    public TimeSpan BatchInterval { get; set; } = TimeSpan.FromSeconds(60);
}
