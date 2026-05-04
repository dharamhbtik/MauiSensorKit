# MauiSensorKit

[![NuGet](https://img.shields.io/nuget/v/ZenithCode.MauiSensorKit.svg)](https://www.nuget.org/packages/ZenithCode.MauiSensorKit/)
[![Build](https://github.com/dharamhbtik/MauiSensorKit/actions/workflows/build-and-publish.yml/badge.svg)](https://github.com/dharamhbtik/MauiSensorKit/actions)

A comprehensive .NET MAUI SDK for collecting, storing, and uploading mobile sensor data. Supports 20+ sensors including accelerometer, gyroscope, GPS, battery, microphone, NFC, UWB, and more.

## Features

- **25+ Sensors**: Accelerometer, Gyroscope, Magnetometer, GPS, Barometer, Battery, Microphone, NFC, UWB, and more
- **Background Recording**: Continue collecting data even when app is in background (with wake lock and battery optimization)
- **Route Tracking**: Real-time GPS route visualization with interactive map
- **Battery Monitoring**: Battery level tracking with graph visualization over time
- **Local Storage**: Save recordings to local device storage
- **Export Options**: Export as formatted text file or compressed ZIP archive
- **Auto Upload**: Automatic upload to your API endpoint with retry logic
- **Activity Recognition**: Detect user activities (walking, running, driving, etc.)
- **Cross-Platform**: Built on `Microsoft.Maui.Devices.Sensors` for native cross-platform support across Android, iOS, Windows, and MacCatalyst where available.

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

## Complete Integration Guide

### Step 1: Install the Package

```bash
dotnet add package MauiSensorKit
```

### Step 2: Configure Platform Permissions

#### Android (AndroidManifest.xml)

Add to `Platforms/Android/AndroidManifest.xml`:

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
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
<uses-permission android:name="android.permission.WAKE_LOCK" />
<uses-permission android:name="android.permission.REQUEST_IGNORE_BATTERY_OPTIMIZATIONS" />

<uses-feature android:name="android.hardware.sensor.accelerometer" android:required="false" />
<uses-feature android:name="android.hardware.sensor.gyroscope" android:required="false" />
<uses-feature android:name="android.hardware.sensor.barometer" android:required="false" />
<uses-feature android:name="android.hardware.nfc" android:required="false" />
<uses-feature android:name="android.hardware.uwb" android:required="false" />
```

#### iOS (Info.plist)

Add to `Platforms/iOS/Info.plist`:

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

### Step 3: Register Services in MauiProgram.cs

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
                options.Enable(SensorType.Magnetometer);
                options.Enable(SensorType.Location);
                options.Enable(SensorType.Battery);
                options.Enable(SensorType.Barometer);
                options.Enable(SensorType.Microphone);
                options.Enable(SensorType.StepCounter);
                
                // Configure sampling rates
                options.MotionSensorSpeed = SensorSpeed.UI;
                options.LocationInterval = TimeSpan.FromSeconds(5);
                options.BatteryPollingInterval = TimeSpan.FromSeconds(30);
                options.MicrophonePollingInterval = TimeSpan.FromSeconds(1);
                
                // Storage settings
                options.EnableLocalStorage = true;
                options.FileNamePrefix = "sensor_data";
                options.MaxLocalFileSizeMB = 50;
                options.MaxLocalFileCount = 10;
            },
            upload =>
            {
                upload.EnableUpload = true;
                upload.ApiEndpointUrl = "https://your-api.example.com/api/sensor-data";
                upload.Headers["Authorization"] = "Bearer YOUR_API_KEY";
                upload.UploadOnlyOnWifi = false;
                upload.DeleteAfterUpload = true;
                upload.EnableCompression = true;
            });
    
    return builder.Build();
}
```

### Step 4: Request Runtime Permissions

For Android, request permissions at runtime:

```csharp
// In your MainActivity.cs or viewmodel
public async Task RequestPermissions()
{
    var status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
    await Permissions.RequestAsync<Permissions.Microphone>();
    
    if (DeviceInfo.Platform == DevicePlatform.Android && 
        DeviceInfo.Version >= new Version(10, 0))
    {
        await Permissions.RequestAsync<Permissions.Sensors>();
    }
}
```

### Step 5: Use the Sensor Service

#### Basic Recording

```csharp
public class SensorViewModel
{
    private readonly ISensorCollectionService _sensorService;
    
    public SensorViewModel(ISensorCollectionService sensorService)
    {
        _sensorService = sensorService;
        _sensorService.ReadingRecorded += OnReadingRecorded;
    }
    
    public async Task StartRecording()
    {
        // For background recording on Android
#if ANDROID
        var context = Android.App.Application.Context;
        MauiSensorKit.Platforms.Android.Services.SensorRecordingService.StartService(context);
#endif
        
        await _sensorService.StartAsync();
    }
    
    public async Task StopRecording()
    {
        await _sensorService.StopAsync();
        
#if ANDROID
        var context = Android.App.Application.Context;
        MauiSensorKit.Platforms.Android.Services.SensorRecordingService.StopService(context);
#endif
    }
    
    private void OnReadingRecorded(object? sender, SensorReading reading)
    {
        Console.WriteLine($"{reading.Type}: {reading}");
    }
}
```

#### Activity Recognition

```csharp
public async Task DetectActivity()
{
    _sensorService.ReadingRecorded += (s, reading) =>
    {
        if (reading is AccelerometerReading accel)
        {
            var magnitude = Math.Sqrt(accel.X * accel.X + accel.Y * accel.Y + accel.Z * accel.Z);
            // Analyze motion to detect walking, running, driving, etc.
        }
    };
    
    await _sensorService.StartAsync();
}
```

#### Export Recordings

```csharp
public class ExportService
{
    private readonly ILocalStorageService _storage;
    
    public ExportService(ILocalStorageService storage)
    {
        _storage = storage;
    }
    
    public async Task<string> ExportToText()
    {
        var path = await _storage.ExportToTextFileAsync();
        Console.WriteLine($"Text export: {path}");
        return path;
    }
    
    public async Task<string> ExportToZip()
    {
        var path = await _storage.ExportToZipAsync();
        Console.WriteLine($"ZIP export: {path}");
        return path;
    }
    
    public string GetExportLocation()
    {
        return _storage.GetExportDirectoryPath();
    }
}
```

### Step 6: Handle Background Recording

For Android background recording, the SDK uses a foreground service. Add to your `AndroidManifest.xml`:

```xml
<service android:name="com.yourcompany.yourapp.SensorRecordingService"
         android:enabled="true"
         android:exported="false"
         android:foregroundServiceType="dataSync|location" />
```

The service shows a persistent notification while recording in background.

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
| `AccelerometerThreshold` | `float` | 0.5 | Minimum change (m/s²) to record |
| `AccelerometerMaxRate` | `int` | 10 | Max recordings per second for accelerometer |
| `LocationInterval` | `TimeSpan` | 5s | GPS update interval |
| `BatteryPollingInterval` | `TimeSpan` | 60s | Battery check interval (1 minute for graph) |
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

## Sample Application Features

The included sample app (`samples/MauiSensorKit.SampleApp`) demonstrates all SDK capabilities:

### Dashboard
- Start/stop sensor recording
- Live sensor readings display
- Storage size and pending upload status
- Export data to text or ZIP

### Route Tracker
- Interactive OpenStreetMap showing GPS route during recording
- Real-time statistics: point count, total distance, session duration
- Route polyline with start/end markers
- Auto-updates every 2 seconds while recording

### Battery Monitor
- Line chart visualization of battery percentage over time
- Shows last 60 readings (~1 hour at default interval)
- Current charge level, state (Charging/Discharging), power source
- Updates every 5 seconds during recording

### Activity Recognition
- Real-time activity detection (Walking, Running, Driving, Stationary)
- Uses accelerometer variance and step detection
- Confidence indicators and motion statistics

### Sensor Selection
- Enable/disable individual sensors
- Configure sampling rates and intervals
- Battery optimization settings

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
