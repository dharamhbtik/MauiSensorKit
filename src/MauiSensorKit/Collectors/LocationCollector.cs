using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Collector for GPS/GNSS location sensor data.
/// </summary>
public sealed class LocationCollector : BaseSensorCollector<LocationCollector>
{
    private string? _sessionId;
    private CancellationTokenSource? _cancellationTokenSource;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocationCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public LocationCollector(ILogger<LocationCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Location;

    /// <inheritdoc/>
    public override async Task<bool> IsSupportedAsync()
    {
        try
        {
            // Try to get last known location to check if location services are available
            var location = await Geolocation.GetLastKnownLocationAsync();
            return true;
        }
        catch (FeatureNotEnabledException)
        {
            return false;
        }
        catch (PermissionException)
        {
            return true; // Permission can be requested later
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking location support");
            return false;
        }
    }

    /// <inheritdoc/>
    public override async Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Location collector is already running");
            return;
        }

        try
        {
            _sessionId = sessionId;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Try to get a quick location to verify services are enabled
            try
            {
                var testLocation = await Geolocation.GetLastKnownLocationAsync();
            }
            catch (FeatureNotEnabledException)
            {
                Logger.LogWarning("Location services are not enabled on this device");
                return;
            }

            IsRunning = true;

            // Start the location polling loop
            _ = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var request = new GeolocationRequest(GeolocationAccuracy.Best, Options.LocationInterval);
                        var location = await Geolocation.GetLocationAsync(request, _cancellationTokenSource.Token);

                        if (location != null)
                        {
                            var reading = new LocationReading
                            {
                                DeviceId = DeviceId,
                                SessionId = _sessionId ?? string.Empty,
                                Latitude = location.Latitude,
                                Longitude = location.Longitude,
                                AltitudeMeters = location.Altitude,
                                AccuracyMeters = location.Accuracy,
                                SpeedMps = location.Speed,
                                CourseDegrees = location.Course,
                                Source = GetLocationSource(location),
                                IsSimulated = DeviceInfo.Current?.DeviceType == DeviceType.Virtual
                            };

                            RaiseReading(reading);
                        }
                    }
                    catch (FeatureNotEnabledException)
                    {
                        Logger.LogWarning("Location services have been disabled");
                        break;
                    }
                    catch (PermissionException)
                    {
                        Logger.LogWarning("Location permission not granted");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error getting location reading");
                    }

                    try
                    {
                        await Task.Delay(Options.LocationInterval, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, _cancellationTokenSource.Token);

            Logger.LogInformation("Location collector started with interval {Interval}", Options.LocationInterval);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting location collector");
            throw;
        }
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Location collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping location collector");
        }

        return Task.CompletedTask;
    }

    private static LocationSource GetLocationSource(Location location)
    {
        // MAUI doesn't expose the source directly, so we infer from accuracy and speed
        if (location.Accuracy < 20)
            return LocationSource.Gps;
        if (location.Accuracy < 100)
            return LocationSource.Network;
        return LocationSource.Unknown;
    }
}
