namespace MauiSensorKit;

/// <summary>
/// Represents a point-in-time battery state for graphing and analytics.
/// </summary>
public class BatterySnapshot
{
    /// <summary>
    /// Timestamp when the snapshot was recorded.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Battery charge level as 0.0-1.0.
    /// </summary>
    public double ChargeLevel { get; set; }
    
    /// <summary>
    /// Battery charge level as percentage (0-100).
    /// </summary>
    public double ChargePercent => ChargeLevel * 100;
    
    /// <summary>
    /// Battery state (Charging, Discharging, Full, etc.).
    /// </summary>
    public BatteryState State { get; set; }
    
    /// <summary>
    /// Battery power source.
    /// </summary>
    public BatteryPowerSource PowerSource { get; set; }
    
    /// <summary>
    /// Battery voltage in volts if available.
    /// </summary>
    public double? VoltageVolts { get; set; }
    
    /// <summary>
    /// Current in milliamps (negative = discharge, positive = charge).
    /// </summary>
    public double? CurrentMilliAmps { get; set; }
    
    /// <summary>
    /// Battery temperature in Celsius if available.
    /// </summary>
    public double? TemperatureCelsius { get; set; }
    
    /// <summary>
    /// Session identifier for grouping snapshots.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Health status derived from temperature.
    /// </summary>
    public BatteryHealthStatus HealthStatus => TemperatureCelsius switch
    {
        null => BatteryHealthStatus.Unknown,
        < 5 => BatteryHealthStatus.Cold,
        > 45 => BatteryHealthStatus.Hot,
        > 40 => BatteryHealthStatus.Warm,
        _ => BatteryHealthStatus.Good
    };
    
    /// <summary>
    /// Estimated remaining minutes until full charge or empty.
    /// </summary>
    public double? EstimatedRemainingMinutes { get; set; }
}

/// <summary>
/// Battery health status based on temperature.
/// </summary>
public enum BatteryHealthStatus
{
    /// <summary>
    /// Battery temperature is normal (under 40°C).
    /// </summary>
    Good,
    
    /// <summary>
    /// Battery is warm (40-45°C).
    /// </summary>
    Warm,
    
    /// <summary>
    /// Battery is hot (over 45°C).
    /// </summary>
    Hot,
    
    /// <summary>
    /// Battery is cold (under 5°C).
    /// </summary>
    Cold,
    
    /// <summary>
    /// Temperature data not available.
    /// </summary>
    Unknown
}
