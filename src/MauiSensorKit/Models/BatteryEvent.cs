namespace MauiSensorKit;

/// <summary>
/// Represents a notable battery event.
/// </summary>
public class BatteryEvent
{
    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Type of battery event.
    /// </summary>
    public BatteryEventType Type { get; set; }
    
    /// <summary>
    /// Human-readable description.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Charge level at the time of event (0.0-1.0).
    /// </summary>
    public double ChargeAtEvent { get; set; }
    
    /// <summary>
    /// Session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the emoji icon for this event type.
    /// </summary>
    public string Icon => Type switch
    {
        BatteryEventType.ChargingStarted => "⚡",
        BatteryEventType.ChargingStopped => "🔌",
        BatteryEventType.ChargingCompleted => "✅",
        BatteryEventType.LowBatteryWarning => "⚠️",
        BatteryEventType.CriticalBattery => "🔴",
        BatteryEventType.OverheatWarning => "🌡️",
        BatteryEventType.FullyCharged => "💚",
        BatteryEventType.PowerSourceChanged => "🔄",
        _ => "🔋"
    };
    
    /// <summary>
    /// Gets the color for this event type.
    /// </summary>
    public string Color => Type switch
    {
        BatteryEventType.ChargingStarted or BatteryEventType.FullyCharged => "#00C896",
        BatteryEventType.ChargingCompleted => "#00C896",
        BatteryEventType.ChargingStopped => "#6B6B8A",
        BatteryEventType.LowBatteryWarning => "#FF8C42",
        BatteryEventType.CriticalBattery => "#FF4757",
        BatteryEventType.OverheatWarning => "#FF4757",
        BatteryEventType.PowerSourceChanged => "#6C63FF",
        _ => "#6B6B8A"
    };
}

/// <summary>
/// Types of battery events.
/// </summary>
public enum BatteryEventType
{
    /// <summary>
    /// Charging has started.
    /// </summary>
    ChargingStarted,
    
    /// <summary>
    /// Charging has stopped.
    /// </summary>
    ChargingStopped,
    
    /// <summary>
    /// Charging completed (reached 100%).
    /// </summary>
    ChargingCompleted,
    
    /// <summary>
    /// Low battery warning (below 20%).
    /// </summary>
    LowBatteryWarning,
    
    /// <summary>
    /// Critical battery (below 10%).
    /// </summary>
    CriticalBattery,
    
    /// <summary>
    /// Battery is overheating (above 45°C).
    /// </summary>
    OverheatWarning,
    
    /// <summary>
    /// Battery reached 100%.
    /// </summary>
    FullyCharged,
    
    /// <summary>
    /// Power source changed (AC/USB/Battery/Wireless).
    /// </summary>
    PowerSourceChanged
}
