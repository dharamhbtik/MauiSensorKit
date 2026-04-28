using MauiSensorKit;

namespace MauiSensorKit.SampleApp.Services;

/// <summary>
/// Connects sensor readings to data stores for background and foreground tracking.
/// </summary>
public sealed class BackgroundDataStoreConnector : IDisposable
{
    private readonly ISensorCollectionService _sensorService;
    private readonly RouteDataStore _routeDataStore;
    private readonly BatteryDataStore _batteryDataStore;
    private readonly SessionStateService _sessionState;

    public BackgroundDataStoreConnector(
        ISensorCollectionService sensorService,
        RouteDataStore routeDataStore,
        BatteryDataStore batteryDataStore,
        SessionStateService sessionState)
    {
        _sensorService = sensorService;
        _routeDataStore = routeDataStore;
        _batteryDataStore = batteryDataStore;
        _sessionState = sessionState;

        _sensorService.ReadingRecorded += OnReadingRecorded;
    }

    private void OnReadingRecorded(object? sender, SensorReading reading)
    {
        switch (reading)
        {
            case LocationReading loc:
                _routeDataStore.AddLocation(loc);
                // Update session state with location
                _sessionState.LastKnownLocation = new RoutePoint
                {
                    Latitude = loc.Latitude,
                    Longitude = loc.Longitude,
                    AltitudeMeters = loc.AltitudeMeters,
                    SpeedMps = loc.SpeedMps,
                    Timestamp = loc.Timestamp,
                    ActivityAtPoint = _sessionState.CurrentActivity
                };
                break;
            case BatteryReading bat:
                _batteryDataStore.AddReading(bat);
                // Update session state with battery level
                _sessionState.CurrentBatteryLevel = bat.ChargeLevel;
                break;
        }
    }

    public void Dispose()
    {
        _sensorService.ReadingRecorded -= OnReadingRecorded;
    }
}
