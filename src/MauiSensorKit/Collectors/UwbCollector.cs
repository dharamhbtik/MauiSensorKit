using Microsoft.Extensions.Logging;

#if ANDROID
using Android.OS;
#endif

#if IOS
using CoreLocation;
using NearbyInteraction;
using Foundation;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for Ultra-Wideband (UWB) sensor data for precise distance measurement.
/// Requires Android 12+ (API 31+) or iOS 11+ with U1 chip.
/// </summary>
public sealed class UwbCollector : BaseSensorCollector<UwbCollector>
{
    private string? _sessionId;

#if IOS
    private NISession? _niSession;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="UwbCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public UwbCollector(ILogger<UwbCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Uwb;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        // Android UWB APIs require reflection as they're not in standard MAUI bindings
        try
        {
            if (Build.VERSION.SdkInt < BuildVersionCodes.S)
            {
                Logger.LogInformation("UWB requires Android 12+ (API 31+)");
                return Task.FromResult(false);
            }

            // Try to get UWB service via reflection
            var context = global::Android.App.Application.Context;
            var uwbService = context.GetSystemService("uwb");
            if (uwbService != null)
            {
                // Check if UWB is enabled via reflection
                var uwbManagerType = uwbService.GetType();
                var isEnabledProp = uwbManagerType.GetProperty("IsUwbEnabled");
                if (isEnabledProp != null)
                {
                    var isEnabled = (bool?)isEnabledProp.GetValue(uwbService) ?? false;
                    return Task.FromResult(isEnabled);
                }
            }
            return Task.FromResult(false);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking UWB support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        // iOS 11+ with U1 chip
        bool isAvailable = NISession.IsSupported;
        if (!isAvailable)
        {
            Logger.LogInformation("UWB requires iOS 11+ with U1 chip (iPhone 11 and later)");
        }
        return Task.FromResult(isAvailable);
#else
        Logger.LogWarning("UWB not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("UWB collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            Logger.LogInformation("UWB on Android requires platform-specific implementation with reflection. Collector started in passive mode.");

#elif IOS
            if (!NISession.IsSupported)
            {
                Logger.LogWarning("UWB is not supported on this iOS device");
                return Task.CompletedTask;
            }

            _niSession = new NISession();
            Logger.LogInformation("UWB collector started. Full UWB ranging requires paired device configuration.");

            // Emit a simulated reading to indicate the collector is active
            var reading = new UwbReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                DistanceMeters = 0,
                AngleDegrees = null,
                PeerDeviceId = "none",
                IsSimulated = true
            };
            RaiseReading(reading);

#else
            Logger.LogWarning("UWB not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting UWB collector");
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task StopAsync()
    {
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }

        try
        {
#if IOS
            _niSession?.Invalidate();
            _niSession = null;
#endif

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("UWB collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping UWB collector");
        }

        return Task.CompletedTask;
    }
}
