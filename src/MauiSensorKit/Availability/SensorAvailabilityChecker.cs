using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Report containing the availability status of all sensors.
/// </summary>
public sealed class SensorAvailabilityReport
{
    /// <summary>
    /// Gets or sets the timestamp when the report was generated.
    /// </summary>
    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets or sets the dictionary of sensor availability statuses.
    /// </summary>
    public Dictionary<SensorType, SensorAvailabilityStatus> Statuses { get; set; } = new();

    /// <summary>
    /// Gets the count of available sensors.
    /// </summary>
    public int AvailableCount => Statuses.Count(s => s.Value == SensorAvailabilityStatus.Available);

    /// <summary>
    /// Gets the count of sensors that require permission.
    /// </summary>
    public int PermissionNeededCount => Statuses.Count(s => s.Value == SensorAvailabilityStatus.PermissionNeeded);

    /// <summary>
    /// Gets the count of unavailable sensors.
    /// </summary>
    public int UnavailableCount => Statuses.Count(s => s.Value == SensorAvailabilityStatus.Unavailable);

    /// <summary>
    /// Gets the count of unsupported sensors.
    /// </summary>
    public int NotSupportedCount => Statuses.Count(s => s.Value == SensorAvailabilityStatus.NotSupported);

    /// <summary>
    /// Checks if a specific sensor is available.
    /// </summary>
    public SensorAvailabilityStatus GetStatus(SensorType sensor)
    {
        return Statuses.TryGetValue(sensor, out var status) ? status : SensorAvailabilityStatus.Unknown;
    }

    /// <summary>
    /// Gets all sensors that are potentially usable (available or need permission).
    /// </summary>
    public IEnumerable<SensorType> GetUsableSensors()
    {
        return Statuses
            .Where(s => s.Value.IsPotentiallyUsable())
            .Select(s => s.Key);
    }
}

/// <summary>
/// Service that checks which sensors are available on the current device.
/// </summary>
public sealed class SensorAvailabilityChecker
{
    private readonly ILogger<SensorAvailabilityChecker> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SensorAvailabilityChecker"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public SensorAvailabilityChecker(ILogger<SensorAvailabilityChecker> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Checks the availability of all sensors and returns a comprehensive report.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="SensorAvailabilityReport"/> containing all sensor statuses.</returns>
    public async Task<SensorAvailabilityReport> CheckAllAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting sensor availability check for all sensors");

        var report = new SensorAvailabilityReport();

        foreach (var sensor in Enum.GetValues<SensorType>())
        {
            try
            {
                var status = await CheckSensorAsync(sensor, cancellationToken);
                report.Statuses[sensor] = status;
                _logger.LogDebug("Sensor {Sensor}: {Status}", sensor, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking availability for sensor {Sensor}", sensor);
                report.Statuses[sensor] = SensorAvailabilityStatus.Unknown;
            }
        }

        _logger.LogInformation(
            "Sensor availability check complete: {Available} available, {Permission} need permission, {Unavailable} unavailable, {NotSupported} not supported",
            report.AvailableCount,
            report.PermissionNeededCount,
            report.UnavailableCount,
            report.NotSupportedCount);

        return report;
    }

    /// <summary>
    /// Checks the availability of a specific sensor.
    /// </summary>
    /// <param name="sensor">The sensor type to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The availability status of the sensor.</returns>
    public Task<SensorAvailabilityStatus> CheckSensorAsync(SensorType sensor, CancellationToken cancellationToken = default)
    {
        return sensor switch
        {
            // Hardware-gated sensors - check actual availability
            SensorType.Camera => CheckCameraAsync(),
            SensorType.DepthSensor => CheckDepthSensorAsync(),
            SensorType.IRSensor => CheckIRSensorAsync(),
            SensorType.FingerprintSensor => CheckFingerprintAsync(),
            SensorType.FaceRecognition => CheckFaceRecognitionAsync(),
            SensorType.HeartRateSensor => CheckHeartRateAsync(),

            // MAUI Essentials sensors
            SensorType.Accelerometer => CheckAccelerometerAsync(),
            SensorType.Gyroscope => CheckGyroscopeAsync(),
            SensorType.Magnetometer => CheckMagnetometerAsync(),
            SensorType.Barometer => CheckBarometerAsync(),
            SensorType.Battery => Task.FromResult(SensorAvailabilityStatus.Available),
            SensorType.Location => CheckLocationAsync(cancellationToken),
            SensorType.Microphone => CheckMicrophoneAsync(),

            // Platform-specific sensors
            SensorType.GravitySensor => CheckGravitySensorAsync(),
            SensorType.LinearAcceleration => CheckLinearAccelerationAsync(),
            SensorType.RotationVector => CheckRotationVectorAsync(),
            SensorType.StepCounter => CheckStepCounterAsync(),
            SensorType.StepDetector => CheckStepDetectorAsync(),
            SensorType.ProximitySensor => CheckProximitySensorAsync(),
            SensorType.AmbientLight => CheckAmbientLightAsync(),
            SensorType.Temperature => CheckTemperatureAsync(),
            SensorType.Humidity => CheckHumidityAsync(),
            SensorType.Nfc => CheckNfcAsync(),
            SensorType.Uwb => CheckUwbAsync(),
            SensorType.HallSensor => CheckHallSensorAsync(),
            SensorType.BatteryTemperature => CheckBatteryTemperatureAsync(),

            _ => Task.FromResult(SensorAvailabilityStatus.Unknown)
        };
    }

    private static Task<SensorAvailabilityStatus> CheckAccelerometerAsync()
    {
        try
        {
            var isSupported = Accelerometer.Default?.IsSupported ?? false;
            return Task.FromResult(isSupported ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
    }

    private static Task<SensorAvailabilityStatus> CheckGyroscopeAsync()
    {
        try
        {
            var isSupported = Gyroscope.Default?.IsSupported ?? false;
            return Task.FromResult(isSupported ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
    }

    private static Task<SensorAvailabilityStatus> CheckMagnetometerAsync()
    {
        try
        {
            var isSupported = Magnetometer.Default?.IsSupported ?? false;
            return Task.FromResult(isSupported ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
    }

    private static Task<SensorAvailabilityStatus> CheckBarometerAsync()
    {
        try
        {
            var isSupported = Barometer.Default?.IsSupported ?? false;
            return Task.FromResult(isSupported ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
    }

    private static async Task<SensorAvailabilityStatus> CheckLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status == PermissionStatus.Granted)
            {
                return SensorAvailabilityStatus.Available;
            }
            else if (status == PermissionStatus.Denied || status == PermissionStatus.Disabled)
            {
                return SensorAvailabilityStatus.PermissionNeeded;
            }
            return SensorAvailabilityStatus.Unavailable;
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown).Result;
        }
    }

    private static async Task<SensorAvailabilityStatus> CheckMicrophoneAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            if (status == PermissionStatus.Granted)
            {
                return SensorAvailabilityStatus.Available;
            }
            else if (status == PermissionStatus.Denied || status == PermissionStatus.Disabled)
            {
                return SensorAvailabilityStatus.PermissionNeeded;
            }
            return SensorAvailabilityStatus.Unavailable;
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown).Result;
        }
    }

    private static Task<SensorAvailabilityStatus> CheckGravitySensorAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Gravity);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS provides gravity via CoreMotion which is generally available on modern devices
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckLinearAccelerationAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.LinearAcceleration);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS provides user acceleration via CoreMotion
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckRotationVectorAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.RotationVector);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS provides device motion via CoreMotion
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckStepCounterAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.StepCounter);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS provides step counting via CMPedometer on modern devices
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckStepDetectorAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.StepDetector);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS provides step detection via CMPedometer
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckProximitySensorAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Proximity);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS provides proximity via UIDevice
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckAmbientLightAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Light);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS does not expose ambient light sensor directly, use brightness approximation
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckTemperatureAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.AmbientTemperature);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#else
        // iOS and other platforms do not expose ambient temperature sensor
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckHumidityAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.RelativeHumidity);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#else
        // iOS and other platforms do not expose humidity sensor
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckNfcAsync()
    {
#if ANDROID
        try
        {
            var nfcAdapter = global::Android.Nfc.NfcAdapter.GetDefaultAdapter(global::Android.App.Application.Context);
            return Task.FromResult(nfcAdapter?.IsEnabled == true ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS supports NFC on iPhone 7 and later
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckUwbAsync()
    {
#if ANDROID
        try
        {
            if (global::Android.OS.Build.VERSION.SdkInt < global::Android.OS.BuildVersionCodes.S)
            {
                return Task.FromResult(SensorAvailabilityStatus.Unavailable);
            }

            // Use reflection to check UWB since Android.Uwb isn't in standard MAUI bindings
            var context = global::Android.App.Application.Context;
            var uwbService = context.GetSystemService("uwb");
            if (uwbService != null)
            {
                var uwbManagerType = uwbService.GetType();
                var isEnabledProp = uwbManagerType.GetProperty("IsUwbEnabled");
                if (isEnabledProp != null)
                {
                    var isEnabled = (bool?)isEnabledProp.GetValue(uwbService) ?? false;
                    return Task.FromResult(isEnabled ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
                }
            }
            return Task.FromResult(SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS supports UWB on iPhone 11 and later with U1 chip
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckCameraAsync()
    {
#if ANDROID
        try
        {
            var context = global::Android.App.Application.Context;
            var pm = context.PackageManager;
            var hasCamera = pm?.HasSystemFeature(global::Android.Content.PM.PackageManager.FeatureCamera) == true;
            return Task.FromResult(hasCamera ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#else
        return Task.FromResult(SensorAvailabilityStatus.Available);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckDepthSensorAsync()
    {
#if ANDROID
        try
        {
            // Check for ARCore depth support or ToF (Time of Flight) sensor
            var context = global::Android.App.Application.Context;
            var pm = context.PackageManager;
            
            // Check for ToF sensor (Proximity is actually IR-based ToF on many devices)
            var sensorManager = context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var hasToFSensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Proximity) != null;
            
            // Check for camera2 depth capability
            var hasDepthCamera = pm?.HasSystemFeature(global::Android.Content.PM.PackageManager.FeatureCameraCapabilitiesDepth) == true ||
                                pm?.HasSystemFeature("android.hardware.camera.ar") == true;
            
            var isAvailable = hasToFSensor || hasDepthCamera;
            return Task.FromResult(isAvailable ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iPhone 12 Pro and later have LiDAR depth sensor
        return Task.FromResult(SensorAvailabilityStatus.Available);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckIRSensorAsync()
    {
#if ANDROID
        try
        {
            // Check for IR blaster (ConsumerIrManager) or IR camera capabilities
            var context = global::Android.App.Application.Context;
            var pm = context.PackageManager;
            
            // Check for IR blaster (most common IR sensor on phones)
            var hasIRBlaster = pm?.HasSystemFeature(global::Android.Content.PM.PackageManager.FeatureConsumerIr) == true;
            
            // Check for IR camera (night vision/thermal)
            var hasIRCamera = pm?.HasSystemFeature("android.hardware.camera.capabilities.ir") == true ||
                             pm?.HasSystemFeature("android.hardware.camera.ir") == true;
            
            // Proximity sensor uses IR, so if that exists, basic IR capability exists
            var sensorManager = context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var hasProximityIR = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.Proximity) != null;
            
            var isAvailable = hasIRBlaster || hasIRCamera || hasProximityIR;
            return Task.FromResult(isAvailable ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#elif IOS
        // iOS devices don't expose IR sensors directly
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckFingerprintAsync()
    {
#if ANDROID
        try
        {
            var context = global::Android.App.Application.Context;
            var fingerprintManager = context.GetSystemService(global::Android.Content.Context.FingerprintService) as global::Android.Hardware.Fingerprints.FingerprintManager;
            var isAvailable = fingerprintManager?.IsHardwareDetected == true && fingerprintManager?.HasEnrolledFingerprints == true;
            return Task.FromResult(isAvailable ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#else
        // iOS uses different API
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckFaceRecognitionAsync()
    {
#if ANDROID
        try
        {
            var context = global::Android.App.Application.Context;
            var biometricManager = context.GetSystemService(global::Android.Content.Context.BiometricService) as global::Android.Hardware.Biometrics.BiometricManager;
            if (biometricManager == null)
                return Task.FromResult(SensorAvailabilityStatus.Unavailable);
            
            // Use reflection to handle different Android versions
            var canAuth = biometricManager.CanAuthenticate();
            var isAvailable = (int)canAuth == 0; // BiometricCode.Success = 0
            return Task.FromResult(isAvailable ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckHeartRateAsync()
    {
#if ANDROID
        try
        {
            var sensorManager = global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.SensorService) as global::Android.Hardware.SensorManager;
            var sensor = sensorManager?.GetDefaultSensor(global::Android.Hardware.SensorType.HeartRate);
            return Task.FromResult(sensor != null ? SensorAvailabilityStatus.Available : SensorAvailabilityStatus.Unavailable);
        }
        catch
        {
            return Task.FromResult(SensorAvailabilityStatus.Unknown);
        }
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckHallSensorAsync()
    {
#if ANDROID
        // Hall sensor is typically available via dock events on Android
        return Task.FromResult(SensorAvailabilityStatus.Available);
#elif IOS
        // iOS does not expose hall sensor
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }

    private static Task<SensorAvailabilityStatus> CheckBatteryTemperatureAsync()
    {
#if ANDROID
        // Battery temperature available via BatteryManager on Android
        return Task.FromResult(SensorAvailabilityStatus.Available);
#elif IOS
        // iOS does not expose battery temperature
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#else
        return Task.FromResult(SensorAvailabilityStatus.Unavailable);
#endif
    }
}
