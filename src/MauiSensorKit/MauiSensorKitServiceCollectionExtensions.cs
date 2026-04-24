using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace MauiSensorKit;

/// <summary>
/// Extension methods for registering MauiSensorKit services with dependency injection.
/// </summary>
public static class MauiSensorKitServiceCollectionExtensions
{
    /// <summary>
    /// Adds MauiSensorKit services to the MauiAppBuilder.
    /// </summary>
    /// <param name="builder">The Maui app builder.</param>
    /// <param name="configureOptions">Optional action to configure sensor kit options.</param>
    /// <param name="configureUpload">Optional action to configure upload options.</param>
    /// <returns>The MauiAppBuilder for chaining.</returns>
    public static MauiAppBuilder UseMauiSensorKit(
        this MauiAppBuilder builder,
        Action<SensorKitOptions>? configureOptions = null,
        Action<SensorKitUploadOptions>? configureUpload = null)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        // 1. Configure options
        builder.Services.Configure<SensorKitOptions>(opts =>
        {
            // Apply defaults first
            var defaults = SensorKitOptions.CreateDefault();
            CopyOptions(defaults, opts);

            // Apply user configuration
            configureOptions?.Invoke(opts);

            // Validate
            var errors = opts.Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Invalid SensorKitOptions: {string.Join(", ", errors)}");
            }
        });

        builder.Services.Configure<SensorKitUploadOptions>(opts =>
        {
            configureUpload?.Invoke(opts);

            var errors = opts.Validate();
            if (errors.Count > 0)
            {
                throw new InvalidOperationException($"Invalid SensorKitUploadOptions: {string.Join(", ", errors)}");
            }
        });

        // 2. Register helpers
        builder.Services.TryAddSingleton<ConnectivityHelper>();
        builder.Services.TryAddSingleton<SensorAvailabilityChecker>();

        // 3. Register ALL collectors as singletons (even unsupported ones)
        builder.Services.TryAddSingleton<BaseSensorCollector, AccelerometerCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, GyroscopeCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, MagnetometerCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, GravitySensorCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, LinearAccelerationCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, RotationVectorCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, StepCounterCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, StepDetectorCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, ProximitySensorCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, AmbientLightCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, BarometerCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, TemperatureCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, HumidityCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, LocationCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, MicrophoneCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, NfcCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, UwbCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, HallSensorCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, BatteryCollector>();
        builder.Services.TryAddSingleton<BaseSensorCollector, BatteryTemperatureCollector>();

        // 4. Register services
        builder.Services.TryAddSingleton<ILocalStorageService, LocalStorageService>();
        builder.Services.AddHttpClient<UploadService>();
        builder.Services.TryAddSingleton<IUploadService, UploadService>();
        builder.Services.TryAddSingleton<ISensorCollectionService, SensorCollectionService>();
        builder.Services.AddHostedService<UploadBackgroundService>();

        return builder;
    }

    private static void CopyOptions(SensorKitOptions source, SensorKitOptions target)
    {
        target.EnabledSensors = new Dictionary<SensorType, bool>(source.EnabledSensors);
        target.MotionSensorSpeed = source.MotionSensorSpeed;
        target.LocationInterval = source.LocationInterval;
        target.BatteryPollingInterval = source.BatteryPollingInterval;
        target.MicrophonePollingInterval = source.MicrophonePollingInterval;
        target.SlowSensorPollingInterval = source.SlowSensorPollingInterval;
        target.EnableLocalStorage = source.EnableLocalStorage;
        target.LocalStoragePath = source.LocalStoragePath;
        target.FileNamePrefix = source.FileNamePrefix;
        target.MaxLocalFileSizeMB = source.MaxLocalFileSizeMB;
        target.MaxLocalFileCount = source.MaxLocalFileCount;
        target.BatchSize = source.BatchSize;
        target.BatchFlushInterval = source.BatchFlushInterval;
    }
}
