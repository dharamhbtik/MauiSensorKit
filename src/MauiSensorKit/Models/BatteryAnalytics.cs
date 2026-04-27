namespace MauiSensorKit;

/// <summary>
/// Computed analytics from a collection of battery snapshots.
/// </summary>
public class BatteryAnalytics
{
    /// <summary>
    /// Session identifier.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// Average discharge rate in percent per hour.
    /// </summary>
    public double AverageDischargeRatePerHour { get; set; }
    
    /// <summary>
    /// Estimated minutes until battery is fully drained at current rate.
    /// </summary>
    public double? EstimatedFullDrainMinutes { get; set; }
    
    /// <summary>
    /// Peak temperature recorded in Celsius.
    /// </summary>
    public double? PeakTemperatureCelsius { get; set; }
    
    /// <summary>
    /// Average temperature in Celsius.
    /// </summary>
    public double? AverageTemperatureCelsius { get; set; }
    
    /// <summary>
    /// Total time spent charging.
    /// </summary>
    public TimeSpan TotalChargingTime { get; set; }
    
    /// <summary>
    /// Total time spent discharging.
    /// </summary>
    public TimeSpan TotalDischargingTime { get; set; }
    
    /// <summary>
    /// Number of charging cycles (start charging events).
    /// </summary>
    public int ChargingCyclesInSession { get; set; }
    
    /// <summary>
    /// Lowest charge level recorded (0.0-1.0).
    /// </summary>
    public double LowestChargeLevel { get; set; } = 1.0;
    
    /// <summary>
    /// Highest charge level recorded (0.0-1.0).
    /// </summary>
    public double HighestChargeLevel { get; set; } = 0.0;
    
    /// <summary>
    /// Notable battery events during the session.
    /// </summary>
    public List<BatteryEvent> Events { get; set; } = new();
    
    /// <summary>
    /// Session start time.
    /// </summary>
    public DateTimeOffset SessionStart { get; set; }
    
    /// <summary>
    /// Session end time.
    /// </summary>
    public DateTimeOffset? SessionEnd { get; set; }
}
