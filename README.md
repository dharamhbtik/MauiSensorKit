# MauiSensorKit

[![NuGet](https://img.shields.io/nuget/v/MauiSensorKit.svg)](https://www.nuget.org/packages/MauiSensorKit/)
[![Build](https://github.com/dharamhbtik/MauiSensorKit/actions/workflows/build-and-publish.yml/badge.svg)](https://github.com/dharamhbtik/MauiSensorKit/actions)

A comprehensive .NET MAUI SDK for collecting, storing, and uploading mobile sensor data. Supports 20+ sensors including accelerometer, gyroscope, GPS, battery, microphone, NFC, UWB, and more.

## Installation

```bash
dotnet add package MauiSensorKit
```

## Quick Start

### 1. Register Services in MauiProgram.cs

```csharp
using MauiSensorKit;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseMauiSensorKit(
            options =>
            {
                // Configure which sensors to enable
                options.Enable(SensorType.Accelerometer);
                options.Enable(SensorType.Gyroscope);
                options.Enable(SensorType.Location);
                options.Enable(SensorType.Battery);
            },
            upload =>
            {
                upload.EnableUpload = true;
                upload.ApiEndpointUrl = "https://your-api.example.com/api/sensor-data";
                upload.Headers["Authorization"] = "Bearer YOUR_API_KEY";
                upload.UploadOnlyOnWifi = false;
                upload.DeleteAfterUpload = true;
            });
    
    return builder.Build();
}
```

### 2. Use the Sensor Collection Service

```csharp
public class MyViewModel
{
    private readonly ISensorCollectionService _sensorService;
    
    public MyViewModel(ISensorCollectionService sensorService)
    {
        _sensorService = sensorService;
        
        // Subscribe to sensor readings
        _sensorService.ReadingRecorded += OnReadingRecorded;
    }
    
    public async Task StartRecording()
    {
        await _sensorService.StartAsync();
    }
    
    public async Task StopRecording()
    {
        await _sensorService.StopAsync();
    }
    
    private void OnReadingRecorded(object? sender, SensorReading reading)
    {
        Console.WriteLine($"Received {reading.Type} reading at {reading.Timestamp}");
    }
}
```

## Supported Sensors

| Sensor | Android | iOS | Notes |
|--------|---------|-----|-------|
| **Motion Sensors** |
| Accelerometer | ✓ | ✓ | m/s² |
| Gyroscope | ✓ | ✓ | rad/s |
| Magnetometer | ✓ | ✓ | µT |
| Gravity | ✓ | ✓ | m/s² via CoreMotion/Gravity sensor |
| Linear Acceleration | ✓ | ✓ | m/s² excluding gravity |
| Rotation Vector | ✓ | ✓ | Quaternion orientation |
| Step Counter | ✓ | ✓ | Cumulative steps |
| Step Detector | ✓ | ✓ | Individual step events |
| **Environment Sensors** |
| Barometer | ✓ | ✓ | Pressure in hPa |
| Ambient Light | ✓ | ⚠️ | Android: Lux sensor, iOS: Screen brightness approximation |
| Temperature | ✓ | ✗ | Android only, rare on phones |
| Humidity | ✓ | ✗ | Android only, rare on phones |
| Proximity | ✓ | ✓ | Distance in cm |
| **Location & Connectivity** |
| Location/GPS | ✓ | ✓ | Lat/Long/Altitude/Speed |
| Microphone | ✓ | ✓ | Amplitude in dB (no raw audio) |
| NFC | ✓ | ✓ | Tag detection events |
| UWB | ✓ (12+) | ✓ | Ultra-wideband ranging |
| **Device Sensors** |
| Battery | ✓ | ✓ | Level/State/Source |
| Battery Temperature | ✓ | ✗ | Android only |
| Hall Sensor | ✓ | ✗ | Magnetic cover detection (dock events) |

### Hardware-Gated Sensors (Not Implemented)

These sensors require dedicated hardware APIs beyond MAUI Essentials scope:

- **Camera** - Use `Microsoft.Maui.Media` or platform-specific camera APIs
- **Depth Sensor** - Requires ARCore/ARKit
- **IR Sensor** - Not exposed via public APIs
- **Fingerprint Sensor** - Use biometric authentication APIs
- **Face Recognition** - Use biometric authentication APIs
- **Heart Rate** - Requires HealthKit/Google Fit

## Configuration Options

### SensorKitOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnabledSensors` | `Dictionary<SensorType, bool>` | All enabled | Which sensors to collect |
| `MotionSensorSpeed` | `SensorSpeed` | `UI` | Sampling rate for motion sensors |
| `LocationInterval` | `TimeSpan` | 5s | GPS update interval |
| `BatteryPollingInterval` | `TimeSpan` | 30s | Battery check interval |
| `MicrophonePollingInterval` | `TimeSpan` | 1s | Audio amplitude sampling |
| `SlowSensorPollingInterval` | `TimeSpan` | 10s | Temp/humidity/etc interval |
| `EnableLocalStorage` | `bool` | `true` | Store data locally |
| `LocalStoragePath` | `string?` | `null` | Custom storage path |
| `FileNamePrefix` | `string` | `sensor_data` | File naming prefix |
| `MaxLocalFileSizeMB` | `int` | 50 | Max file size |
| `MaxLocalFileCount` | `int` | 10 | Max number of files |
| `BatchSize` | `int` | 100 | Readings per batch |
| `BatchFlushInterval` | `TimeSpan` | 30s | Auto-flush interval |

### SensorKitUploadOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `EnableUpload` | `bool` | `false` | Enable automatic upload |
| `ApiEndpointUrl` | `string?` | `null` | API endpoint for uploads |
| `Headers` | `Dictionary<string, string>` | Empty | Custom HTTP headers |
| `UploadRetryInterval` | `TimeSpan` | 60s | Time between upload attempts |
| `MaxRetryAttempts` | `int` | 3 | Max retries per batch |
| `UploadOnlyOnWifi` | `bool` | `false` | Only upload on WiFi |
| `DeleteAfterUpload` | `bool` | `true` | Delete local files after upload |
| `UploadTimeout` | `TimeSpan` | 30s | HTTP timeout |
| `EnableCompression` | `bool` | `true` | Compress upload data |

## API Upload Payload

The upload service sends data to your API in this JSON format:

```json
{
  "$type": "accelerometer",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2024-01-15T10:30:00.000Z",
  "deviceId": "abc123def456",
  "sessionId": "session789xyz",
  "isSimulated": false,
  "x": 0.123,
  "y": -0.456,
  "z": 9.789
}
```

Batches are sent as:

```json
{
  "id": "batch-uuid",
  "sessionId": "session789xyz",
  "deviceId": "abc123def456",
  "batchCreatedAt": "2024-01-15T10:30:00.000Z",
  "readings": [
    // Array of SensorReading objects with $type discriminator
  ],
  "isUploaded": false,
  "readingCount": 100
}
```

## Permissions

### Android (AndroidManifest.xml)

```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
<uses-permission android:name="android.permission.BODY_SENSORS" />
<uses-permission android:name="android.permission.ACTIVITY_RECOGNITION" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.NFC" />
<uses-permission android:name="android.permission.UWB_RANGING" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE" />
<uses-permission android:name="android.permission.FOREGROUND_SERVICE_LOCATION" />

<uses-feature android:name="android.hardware.sensor.accelerometer" android:required="false" />
<uses-feature android:name="android.hardware.sensor.gyroscope" android:required="false" />
<uses-feature android:name="android.hardware.sensor.barometer" android:required="false" />
<uses-feature android:name="android.hardware.nfc" android:required="false" />
<uses-feature android:name="android.hardware.uwb" android:required="false" />
```

### iOS (Info.plist)

```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>Used to record GPS location sensor data</string>
<key>NSLocationAlwaysAndWhenInUseUsageDescription</key>
<string>Used for background sensor recording</string>
<key>NSMotionUsageDescription</key>
<string>Used to access accelerometer, gyroscope, and motion sensors</string>
<key>NSMicrophoneUsageDescription</key>
<string>Used to measure ambient sound levels only (no recording)</string>
<key>NFCReaderUsageDescription</key>
<string>Used to detect nearby NFC tags</string>
```

## Building from Source

### Prerequisites
- .NET 10 SDK
- MAUI workload installed
- Android SDK (for Android builds)
- Xcode (for iOS builds on macOS)

### Build Commands

```bash
# Clone the repository
git clone https://github.com/dharamhbtik/MauiSensorKit.git
cd MauiSensorKit

# Build the library
dotnet build src/MauiSensorKit/MauiSensorKit.csproj -c Release

# Run the sample app (Android)
dotnet build samples/MauiSensorKit.SampleApp/MauiSensorKit.SampleApp.csproj -f net10.0-android -t:Run

# Create NuGet package
dotnet pack src/MauiSensorKit/MauiSensorKit.csproj -c Release
```

### Project Structure

```
MauiSensorKit/
├── src/
│   └── MauiSensorKit/           # Main library
│       ├── Collectors/          # Sensor data collectors
│       ├── Models/              # Data models
│       ├── Services/            # Storage and upload services
│       └── Enums/               # Enums and types
├── samples/
│   └── MauiSensorKit.SampleApp/ # Sample application
└── .github/workflows/           # CI/CD automation
```

## CI/CD

This project uses GitHub Actions for automated builds and publishing:

- **Build**: Triggered on every push and pull request
- **Publish**: Automatically publishes NuGet package when a version tag (v*) is pushed
- **Artifacts**: Build artifacts are stored for 30 days

To publish a new version:

```bash
# Update version in src/MauiSensorKit/MauiSensorKit.csproj
git add .
git commit -m "Version bump"
git tag -a v1.0.1 -m "Release v1.0.1"
git push origin main --tags
```

## License

MIT License - See LICENSE file for details.
