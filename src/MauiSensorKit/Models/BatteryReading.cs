namespace MauiSensorKit;

/// <summary>
/// Represents a battery sensor reading including charge level and state.
/// </summary>
public sealed record BatteryReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Battery;

    /// <summary>
    /// Gets the battery charge level as a value from 0.0 (empty) to 1.0 (full).
    /// </summary>
    public double ChargeLevel { get; init; }

    /// <summary>
    /// Gets the battery charge level as a percentage (0-100).
    /// </summary>
    public int ChargePercentage => (int)(ChargeLevel * 100);

    /// <summary>
    /// Gets the current battery state (charging, discharging, etc.).
    /// </summary>
    public BatteryState State { get; init; }

    /// <summary>
    /// Gets the current power source.
    /// </summary>
    public BatteryPowerSource PowerSource { get; init; }
    
    /// <summary>
    /// Gets the battery voltage in volts if available.
    /// </summary>
    public double? VoltageVolts { get; init; }
    
    /// <summary>
    /// Gets the current in milliamps (negative = discharge, positive = charge).
    /// </summary>
    public double? CurrentMilliAmps { get; init; }
    
    /// <summary>
    /// Gets the battery temperature in Celsius if available.
    /// </summary>
    public double? TemperatureCelsius { get; init; }
    
    /// <summary>
    /// Gets the estimated remaining minutes until full charge or empty.
    /// </summary>
    public double? EstimatedRemainingMinutes { get; init; }
    
    /// <summary>
    /// Gets the battery health status based on temperature.
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
    /// Gets a value indicating whether the device is currently charging.
    /// </summary>
    public bool IsCharging => State == BatteryState.Charging;

    /// <summary>
    /// Gets the battery technology (e.g., "Li-ion", "Li-poly").
    /// </summary>
    public string Technology { get; init; } = "Unknown";

    /// <summary>
    /// Gets the battery health status (Good, Cold, Dead, Overheat, etc.).
    /// </summary>
    public BatteryHealth Health { get; init; } = BatteryHealth.Unknown;

    /// <summary>
    /// Gets the remaining capacity in milliWatt-hours (mWh).
    /// </summary>
    public int? CapacityRemainingMWh { get; init; }

    /// <summary>
    /// Gets the battery capacity percentage as reported by system.
    /// </summary>
    public double? BatteryCapacityPercent { get; init; }



    /// <summary>
    /// Converts to a BatterySnapshot for storage.
    /// </summary>
    public BatterySnapshot ToSnapshot()
    {
        return new BatterySnapshot
        {
            Timestamp = Timestamp,
            ChargeLevel = ChargeLevel,
            State = State,
            PowerSource = PowerSource,
            VoltageVolts = VoltageVolts,
            CurrentMilliAmps = CurrentMilliAmps,
            TemperatureCelsius = TemperatureCelsius,
            SessionId = SessionId,
            EstimatedRemainingMinutes = EstimatedRemainingMinutes,
            Technology = Technology,
            Health = Health,
            CapacityRemainingMWh = CapacityRemainingMWh,
            BatteryCapacityPercent = BatteryCapacityPercent
        };
    }

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        var charging = IsCharging ? " (Charging)" : "";
        return $"Battery: {ChargePercentage}% - {State} [{PowerSource}]{charging}";
    }
}
