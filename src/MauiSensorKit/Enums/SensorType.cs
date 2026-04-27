namespace MauiSensorKit;

/// <summary>
/// Represents all sensor types supported by MauiSensorKit.
/// </summary>
public enum SensorType
{
    /// <summary>
    /// Detects motion, tilt, shaking, and screen orientation.
    /// </summary>
    Accelerometer,

    /// <summary>
    /// Detects rotation and angular movement.
    /// </summary>
    Gyroscope,

    /// <summary>
    /// Digital compass via Earth's magnetic field.
    /// </summary>
    Magnetometer,

    /// <summary>
    /// Measures gravity direction for phone orientation.
    /// </summary>
    GravitySensor,

    /// <summary>
    /// Movement speed excluding gravity effects.
    /// </summary>
    LinearAcceleration,

    /// <summary>
    /// Fuses accelerometer, gyroscope, and magnetometer for accurate orientation.
    /// </summary>
    RotationVector,

    /// <summary>
    /// Cumulative step count since last device reboot.
    /// </summary>
    StepCounter,

    /// <summary>
    /// Real-time individual step detection event.
    /// </summary>
    StepDetector,

    /// <summary>
    /// Detects nearby objects (e.g., ear during phone call).
    /// </summary>
    ProximitySensor,

    /// <summary>
    /// Measures ambient light in lux for automatic screen brightness.
    /// </summary>
    AmbientLight,

    /// <summary>
    /// Air pressure in hectopascals for altitude and weather.
    /// </summary>
    Barometer,

    /// <summary>
    /// Device or ambient temperature in Celsius.
    /// </summary>
    Temperature,

    /// <summary>
    /// Relative humidity percentage (rare on mobile devices).
    /// </summary>
    Humidity,

    /// <summary>
    /// GPS/GNSS latitude, longitude, altitude, and speed.
    /// </summary>
    Location,

    /// <summary>
    /// Audio amplitude / sound level in dB (not raw PCM audio).
    /// </summary>
    Microphone,

    /// <summary>
    /// NFC tag detection events.
    /// </summary>
    Nfc,

    /// <summary>
    /// Ultra-wideband ranging for precise distance measurement.
    /// </summary>
    Uwb,

    /// <summary>
    /// Magnetic cover/flip case detection.
    /// </summary>
    HallSensor,

    /// <summary>
    /// Battery charge level, state, and power source.
    /// </summary>
    Battery,

    /// <summary>
    /// Battery temperature in Celsius for safety monitoring.
    /// </summary>
    BatteryTemperature,

    /// <summary>
    /// Camera sensor for image capture. Supported via external camera APIs.
    /// </summary>
    [NotImplemented("Camera: Use .NET MAUI CommunityToolkit CameraView or platform camera APIs")]
    Camera,

    /// <summary>
    /// Depth sensor for 3D scene reconstruction. Supported via ARCore/ARKit.
    /// </summary>
    [NotImplemented("Depth: Use ARCore (Android) or ARKit (iOS) frameworks")]
    DepthSensor,

    /// <summary>
    /// Infrared sensor for proximity/distance. Supported via proximity APIs.
    /// </summary>
    [NotImplemented("IR: Use ProximitySensor or platform IR APIs")]
    IRSensor,

    /// <summary>
    /// Fingerprint biometric sensor. Supported via biometric APIs.
    /// </summary>
    [NotImplemented("Fingerprint: Use .NET MAUI BiometricAuthentication or platform APIs")]
    FingerprintSensor,

    /// <summary>
    /// Face recognition sensor. Supported via biometric APIs.
    /// </summary>
    [NotImplemented("Face: Use .NET MAUI BiometricAuthentication or platform APIs")]
    FaceRecognition,

    /// <summary>
    /// Heart rate sensor for health monitoring. Supported via Health platform.
    /// </summary>
    [NotImplemented("Heart Rate: Use HealthKit (iOS) or Health Connect (Android)")]
    HeartRateSensor
}

/// <summary>
/// Attribute to mark sensors that are not implemented in MauiSensorKit.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class NotImplementedAttribute : Attribute
{
    /// <summary>
    /// Gets the reason why this sensor is not implemented.
    /// </summary>
    public string Reason { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotImplementedAttribute"/> class.
    /// </summary>
    /// <param name="reason">The reason why the sensor is not implemented.</param>
    public NotImplementedAttribute(string reason)
    {
        Reason = reason;
    }
}

/// <summary>
/// Extension methods for SensorType.
/// </summary>
public static class SensorTypeExtensions
{
    /// <summary>
    /// Gets the human-readable description for a sensor type.
    /// </summary>
    public static string GetDescription(this SensorType sensor)
    {
        return sensor switch
        {
            SensorType.Accelerometer => "Detects motion, tilt, shaking, screen orientation",
            SensorType.Gyroscope => "Detects rotation and angular movement",
            SensorType.Magnetometer => "Digital compass via Earth's magnetic field",
            SensorType.GravitySensor => "Measures gravity direction for phone orientation",
            SensorType.LinearAcceleration => "Movement speed excluding gravity effects",
            SensorType.RotationVector => "Fuses Accel+Gyro+Mag for accurate orientation",
            SensorType.StepCounter => "Cumulative step count since last reboot",
            SensorType.StepDetector => "Real-time individual step detection event",
            SensorType.ProximitySensor => "Detects nearby objects (e.g. ear during call)",
            SensorType.AmbientLight => "Measures lux for auto screen brightness",
            SensorType.Barometer => "Air pressure (hPa) for altitude/weather",
            SensorType.Temperature => "Device or ambient temperature (°C)",
            SensorType.Humidity => "Relative humidity % (rare on phones)",
            SensorType.Location => "GPS/GNSS latitude, longitude, altitude, speed",
            SensorType.Microphone => "Audio amplitude / sound level (dB) - not raw PCM",
            SensorType.Nfc => "NFC tag detection events",
            SensorType.Uwb => "Ultra-wideband ranging distance",
            SensorType.HallSensor => "Magnetic cover/flip case detection",
            SensorType.Battery => "Charge level, state, power source",
            SensorType.BatteryTemperature => "Battery heat in °C for safety monitoring",
            SensorType.Camera => "Camera image capture (via external APIs)",
            SensorType.DepthSensor => "3D depth sensing (via ARCore/ARKit)",
            SensorType.IRSensor => "Infrared proximity (via platform APIs)",
            SensorType.FingerprintSensor => "Fingerprint biometrics (via Biometric APIs)",
            SensorType.FaceRecognition => "Face recognition (via Biometric APIs)",
            SensorType.HeartRateSensor => "Heart rate monitor (via Health platform)",
            _ => sensor.ToString()
        };
    }

    /// <summary>
    /// Gets an icon name suggestion for the sensor type.
    /// </summary>
    public static string GetIconName(this SensorType sensor)
    {
        return sensor switch
        {
            SensorType.Accelerometer => "accelerometer",
            SensorType.Gyroscope => "gyroscope",
            SensorType.Magnetometer => "magnetometer",
            SensorType.GravitySensor => "gravity",
            SensorType.LinearAcceleration => "linear_accel",
            SensorType.RotationVector => "rotation",
            SensorType.StepCounter => "step_counter",
            SensorType.StepDetector => "step_detector",
            SensorType.ProximitySensor => "proximity",
            SensorType.AmbientLight => "light",
            SensorType.Barometer => "barometer",
            SensorType.Temperature => "temperature",
            SensorType.Humidity => "humidity",
            SensorType.Location => "location",
            SensorType.Microphone => "microphone",
            SensorType.Nfc => "nfc",
            SensorType.Uwb => "uwb",
            SensorType.HallSensor => "lock",
            SensorType.Battery => "battery",
            SensorType.BatteryTemperature => "battery_temp",
            SensorType.Camera => "camera",
            SensorType.DepthSensor => "depth",
            SensorType.IRSensor => "infrared",
            SensorType.FingerprintSensor => "fingerprint",
            SensorType.FaceRecognition => "face",
            SensorType.HeartRateSensor => "heart",
            _ => "unknown"
        };
    }

    /// <summary>
    /// Determines if the sensor is a hardware-gated (not implemented) sensor.
    /// </summary>
    public static bool IsHardwareGated(this SensorType sensor)
    {
        return sensor is SensorType.Camera or
               SensorType.DepthSensor or
               SensorType.IRSensor or
               SensorType.FingerprintSensor or
               SensorType.FaceRecognition or
               SensorType.HeartRateSensor;
    }

    /// <summary>
    /// Gets the category group for the sensor type.
    /// </summary>
    public static string GetCategory(this SensorType sensor)
    {
        return sensor switch
        {
            SensorType.Accelerometer or
            SensorType.Gyroscope or
            SensorType.Magnetometer or
            SensorType.GravitySensor or
            SensorType.LinearAcceleration or
            SensorType.RotationVector or
            SensorType.StepCounter or
            SensorType.StepDetector => "Motion Sensors",

            SensorType.Barometer or
            SensorType.AmbientLight or
            SensorType.Temperature or
            SensorType.Humidity or
            SensorType.ProximitySensor => "Environment Sensors",

            SensorType.Location or
            SensorType.Microphone or
            SensorType.Nfc or
            SensorType.Uwb => "Location & Connectivity",

            SensorType.Battery or
            SensorType.BatteryTemperature or
            SensorType.HallSensor => "Device",

            _ => "Security & Biometrics"
        };
    }
}
