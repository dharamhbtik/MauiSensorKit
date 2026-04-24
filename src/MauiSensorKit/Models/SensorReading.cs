using System.Text.Json.Serialization;

namespace MauiSensorKit;

/// <summary>
/// Abstract base class for all sensor readings.
/// This class serves as the polymorphic base for JSON serialization.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(AccelerometerReading), "accelerometer")]
[JsonDerivedType(typeof(GyroscopeReading), "gyroscope")]
[JsonDerivedType(typeof(MagnetometerReading), "magnetometer")]
[JsonDerivedType(typeof(GravitySensorReading), "gravity")]
[JsonDerivedType(typeof(LinearAccelerationReading), "linearAcceleration")]
[JsonDerivedType(typeof(RotationVectorReading), "rotationVector")]
[JsonDerivedType(typeof(StepCounterReading), "stepCounter")]
[JsonDerivedType(typeof(StepDetectorReading), "stepDetector")]
[JsonDerivedType(typeof(ProximitySensorReading), "proximity")]
[JsonDerivedType(typeof(AmbientLightReading), "ambientLight")]
[JsonDerivedType(typeof(BarometerReading), "barometer")]
[JsonDerivedType(typeof(TemperatureReading), "temperature")]
[JsonDerivedType(typeof(HumidityReading), "humidity")]
[JsonDerivedType(typeof(LocationReading), "location")]
[JsonDerivedType(typeof(MicrophoneReading), "microphone")]
[JsonDerivedType(typeof(NfcReading), "nfc")]
[JsonDerivedType(typeof(UwbReading), "uwb")]
[JsonDerivedType(typeof(HallSensorReading), "hallSensor")]
[JsonDerivedType(typeof(BatteryReading), "battery")]
[JsonDerivedType(typeof(BatteryTemperatureReading), "batteryTemperature")]
public abstract record SensorReading
{
    /// <summary>
    /// Gets the unique identifier for this reading.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the timestamp when the reading was taken.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the unique identifier of the device that took the reading.
    /// This is a persisted GUID stored in device preferences.
    /// </summary>
    public required string DeviceId { get; init; }

    /// <summary>
    /// Gets the session identifier for this reading.
    /// All readings in the same collection session share the same session ID.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Gets the type of sensor that produced this reading.
    /// </summary>
    public abstract SensorType Type { get; }

    /// <summary>
    /// Gets a value indicating whether this reading was simulated (e.g., from an emulator or static value).
    /// </summary>
    public bool IsSimulated { get; init; }
}
