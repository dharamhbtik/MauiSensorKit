using System.Text.Json.Serialization;

namespace MauiSensorKit;

/// <summary>
/// JSON serialization context for all sensor data types.
/// This enables AOT-safe JSON serialization and deserialization.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultBufferSize = 4096,
    WriteIndented = true)]
[JsonSerializable(typeof(SensorDataBatch))]
[JsonSerializable(typeof(SensorReading))]
[JsonSerializable(typeof(AccelerometerReading))]
[JsonSerializable(typeof(GyroscopeReading))]
[JsonSerializable(typeof(MagnetometerReading))]
[JsonSerializable(typeof(GravitySensorReading))]
[JsonSerializable(typeof(LinearAccelerationReading))]
[JsonSerializable(typeof(RotationVectorReading))]
[JsonSerializable(typeof(StepCounterReading))]
[JsonSerializable(typeof(StepDetectorReading))]
[JsonSerializable(typeof(ProximitySensorReading))]
[JsonSerializable(typeof(AmbientLightReading))]
[JsonSerializable(typeof(BarometerReading))]
[JsonSerializable(typeof(TemperatureReading))]
[JsonSerializable(typeof(HumidityReading))]
[JsonSerializable(typeof(LocationReading))]
[JsonSerializable(typeof(MicrophoneReading))]
[JsonSerializable(typeof(NfcReading))]
[JsonSerializable(typeof(UwbReading))]
[JsonSerializable(typeof(HallSensorReading))]
[JsonSerializable(typeof(BatteryReading))]
[JsonSerializable(typeof(BatteryTemperatureReading))]
[JsonSerializable(typeof(SensorManifest))]
[JsonSerializable(typeof(BatchManifestEntry))]
[JsonSerializable(typeof(List<SensorDataBatch>))]
[JsonSerializable(typeof(Dictionary<SensorType, bool>))]
public partial class SensorDataJsonContext : JsonSerializerContext
{
}
