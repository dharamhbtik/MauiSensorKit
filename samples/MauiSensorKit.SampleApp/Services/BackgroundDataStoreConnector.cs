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

    public BackgroundDataStoreConnector(
        ISensorCollectionService sensorService,
        RouteDataStore routeDataStore,
        BatteryDataStore batteryDataStore)
    {
        _sensorService = sensorService;
        _routeDataStore = routeDataStore;
        _batteryDataStore = batteryDataStore;

        _sensorService.ReadingRecorded += OnReadingRecorded;
    }

    private void OnReadingRecorded(object? sender, SensorReading reading)
    {
        switch (reading)
        {
            case LocationReading loc:
                _routeDataStore.AddLocation(loc);
                break;
            case BatteryReading bat:
                _batteryDataStore.AddReading(bat);
                break;
        }
    }

    public void Dispose()
    {
        _sensorService.ReadingRecorded -= OnReadingRecorded;
    }
}
