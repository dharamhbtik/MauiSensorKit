# MauiSensorKit

[![NuGet](https://img.shields.io/nuget/v/ZenithCode.MauiSensorKit.svg)](https://www.nuget.org/packages/ZenithCode.MauiSensorKit/)
[![Build](https://github.com/dharamhbtik/MauiSensorKit/actions/workflows/build-and-publish.yml/badge.svg)](https://github.com/dharamhbtik/MauiSensorKit/actions)

A comprehensive .NET MAUI SDK for collecting, storing, and uploading mobile sensor data. Supports 20+ sensors including accelerometer, gyroscope, GPS, battery, microphone, NFC, UWB, and more.

## ✨ What's New in v1.2.0

- **🔥 Firebase Upload**: Upload sensor batches directly to Firebase Realtime Database — no Firebase SDK dependency required.
- **📦 GZip Compression**: Payloads are now compressed with `GZipStream` before upload, significantly reducing bandwidth usage.
- **🎙️ Automated Background Recording**: New `ISensorRecordingService` — configure which sensors to record, set a batch interval, and the SDK handles buffering, batching, and flushing to local storage automatically.
- **📤 Flexible Upload Targets**: Choose between `CustomApi` (HTTP POST to your endpoint) or `Firebase` (REST API PUT to your Realtime Database).
- **💾 Built-in Export**: Export all local recordings as a compressed ZIP or human-readable text file directly through the recording service.
- **🔋 Battery Collector Modernized**: Android battery hardware metrics (voltage, current, temperature, health) now use the modern `BatteryProperty` API instead of deprecated intent extras.

## Features

- **25+ Sensors**: Accelerometer, Gyroscope, Magnetometer, GPS, Barometer, Battery, Microphone, NFC, UWB, and more
- **Automated Background Recording**: Configure sensors and batch interval — the SDK captures, buffers, and stores data automatically
- **Route Tracking**: Real-time GPS route visualization with interactive map
- **Battery Monitoring**: Battery level tracking with graph visualization over time, including hardware metrics (voltage, current, temperature, health) on Android
- **Local Storage**: Save recordings to local device storage with manifest tracking
- **Export Options**: Export as formatted text file or compressed ZIP archive
- **Firebase Upload**: Upload sensor data directly to Firebase Realtime Database (no SDK dependency)
- **Custom API Upload**: Automatic upload to your API endpoint with retry logic, exponential backoff, and GZip compression
- **Offline-First**: Data is always saved locally first; uploads happen when connectivity is available
- **Activity Recognition**: Detect user activities (walking, running, driving, etc.)
- **Cross-Platform**: Built on `Microsoft.Maui.Devices.Sensors` for native cross-platform support across Android, iOS, Windows, and MacCatalyst where available.

## Installation

```bash
dotnet add package ZenithCode.MauiSensorKit
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

## Automated Background Recording (New in v1.2.0)

The `ISensorRecordingService` handles the entire capture lifecycle — subscribing to sensors, buffering readings in memory, and flushing them to batched JSON files on a configurable timer.

### Setup

```csharp
builder.UseMauiSensorKit(
    configureOptions: options =>
    {
        options.Enable(SensorType.Accelerometer);
        options.Enable(SensorType.Gyroscope);
        options.Enable(SensorType.Battery);
        options.Enable(SensorType.Location);
    },
    configureRecording: recording =>
    {
        // Which sensors to buffer for batch recording
        recording.SensorsToRecord = new HashSet<SensorType>
        {
            SensorType.Accelerometer,
            SensorType.Gyroscope,
            SensorType.Battery
        };
        
        // Flush buffer to local storage every 60 seconds
        recording.BatchInterval = TimeSpan.FromSeconds(60);
    });
```

### Usage

```csharp
public class RecordingViewModel
{
    private readonly ISensorRecordingService _recordingService;
    
    public RecordingViewModel(ISensorRecordingService recordingService)
    {
        _recordingService = recordingService;
    }
    
    public async Task StartRecording()
    {
        // Starts sensor collection + automatic background batching
        await _recordingService.StartRecordingAsync();
    }
    
    public async Task StopRecording()
    {
        // Stops sensors, flushes remaining buffer to storage
        await _recordingService.StopRecordingAsync();
    }
    
    public async Task ExportData()
    {
        // Export all locally stored batches as a ZIP
        string zipPath = await _recordingService.ExportRecordingsToZipAsync();
        
        // Or export as a human-readable text summary
        string textPath = await _recordingService.ExportRecordingsToTextAsync();
    }
}
```

## Upload Targets (New in v1.2.0)

### Option A: Custom API

Upload to your own REST API endpoint via HTTP POST:

```csharp
builder.UseMauiSensorKit(
    configureUpload: upload =>
    {
        upload.EnableUpload = true;
        upload.Target = UploadTarget.CustomApi;
        upload.ApiEndpointUrl = "https://your-api.example.com/api/sensor-data";
        upload.Headers["Authorization"] = "Bearer YOUR_API_KEY";
        upload.EnableCompression = true;  // GZip the payload
        upload.UploadOnlyOnWifi = false;
        upload.DeleteAfterUpload = true;
    });
```

### Option B: Firebase Realtime Database

Upload directly to Firebase without requiring the Firebase SDK:

```csharp
builder.UseMauiSensorKit(
    configureUpload: upload =>
    {
        upload.EnableUpload = true;
        upload.Target = UploadTarget.Firebase;
        upload.FirebaseDatabaseUrl = "https://your-project-id.firebaseio.com";
        upload.FirebaseAuthToken = "YOUR_FIREBASE_AUTH_TOKEN"; // optional
        upload.EnableCompression = true;
        upload.UploadOnlyOnWifi = true;
    });
```

Data is written to: `{FirebaseDatabaseUrl}/sensor_batches/{sessionId}_{batchId}.json`

### Option C: Local Only (No Upload)

If no upload target is configured, all data is saved locally on the device. Use the export API to let your users download their data:

```csharp
builder.UseMauiSensorKit(
    configureUpload: upload =>
    {
        upload.EnableUpload = false; // default
    },
    configureRecording: recording =>
    {
        recording.SensorsToRecord = new HashSet<SensorType>
        {
            SensorType.Accelerometer,
            SensorType.Battery
        };
        recording.BatchInterval = TimeSpan.FromSeconds(60);
    });

// Later, export via ISensorRecordingService
var zipPath = await recordingService.ExportRecordingsToZipAsync();
await Share.Default.RequestAsync(new ShareFileRequest
{
    Title = "Sensor Recordings",
    File = new ShareFile(zipPath)
});
```

### Offline-First Architecture

Regardless of the upload target, the SDK **always saves data locally first**. The background `UploadBackgroundService` runs on a timer and:

1. Checks connectivity (respects `UploadOnlyOnWifi`).
2. Loads pending (un-uploaded) batches from the local manifest.
3. Uploads each batch with retry + exponential backoff.
4. Marks batches as uploaded (and optionally deletes them).

If the device goes offline, batches accumulate locally and are uploaded when connectivity is restored.

## Complete Integration Guide

### Step 1: Install the Package

```bash
dotnet add package ZenithCode.MauiSensorKit
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
                upload.Target = UploadTarget.CustomApi;
                upload.ApiEndpointUrl = "https://your-api.example.com/api/sensor-data";
                upload.Headers["Authorization"] = "Bearer YOUR_API_KEY";
                upload.UploadOnlyOnWifi = false;
                upload.DeleteAfterUpload = true;
                upload.EnableCompression = true;
            },
            recording =>
            {
                recording.SensorsToRecord = new HashSet<SensorType>
                {
                    SensorType.Accelerometer,
                    SensorType.Gyroscope,
                    SensorType.Battery
                };
                recording.BatchInterval = TimeSpan.FromSeconds(60);
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
    private readonly ISensorRecordingService _recording;
    
    public ExportService(ISensorRecordingService recording)
    {
        _recording = recording;
    }
    
    public async Task<string> ExportToZip()
    {
        var path = await _recording.ExportRecordingsToZipAsync();
        Console.WriteLine($"ZIP export: {path}");
        return path;
    }
    
    public async Task<string> ExportToText()
    {
        var path = await _recording.ExportRecordingsToTextAsync();
        Console.WriteLine($"Text export: {path}");
        return path;
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
| Battery | ✓ | ✓ | Level/State/Source + Voltage/Current/Temperature/Health on Android |
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
| `Target` | `UploadTarget` | `CustomApi` | Upload destination: `CustomApi` or `Firebase` |
| `ApiEndpointUrl` | `string?` | `null` | API endpoint for custom API uploads |
| `FirebaseDatabaseUrl` | `string?` | `null` | Firebase Realtime Database URL (e.g., `https://project.firebaseio.com`) |
| `FirebaseAuthToken` | `string?` | `null` | Optional Firebase auth token |
| `Headers` | `Dictionary<string, string>` | Empty | Custom HTTP headers (for custom API) |
| `UploadRetryInterval` | `TimeSpan` | 60s | Time between upload attempts |
| `MaxRetryAttempts` | `int` | 3 | Max retries per batch |
| `UploadOnlyOnWifi` | `bool` | `false` | Only upload on WiFi |
| `DeleteAfterUpload` | `bool` | `true` | Delete local files after upload |
| `UploadTimeout` | `TimeSpan` | 30s | HTTP timeout |
| `EnableCompression` | `bool` | `true` | GZip compress upload payloads |

### SensorRecordingOptions

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AutoStart` | `bool` | `false` | Auto-start recording on service creation |
| `SensorsToRecord` | `HashSet<SensorType>` | Accelerometer, Gyroscope, Battery | Which sensors to buffer and batch |
| `BatchInterval` | `TimeSpan` | 60s | How often to flush the in-memory buffer to local storage |

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

When `EnableCompression` is `true`, the JSON payload is GZip-compressed and sent with `Content-Encoding: gzip` header.

For Firebase, data is written via `PUT` to `{FirebaseDatabaseUrl}/sensor_batches/{sessionId}_{batchId}.json`.

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
- Hardware metrics on Android: voltage, current draw, temperature, health status
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
│       ├── Configuration/       # Options classes (SensorKitOptions, UploadOptions, RecordingOptions)
│       ├── Models/              # Data models
│       ├── Services/            # Storage, upload, and recording services
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
git commit -m "Release v1.2.0"
git tag -a v1.2.0 -m "Release v1.2.0"
git push origin main --tags
```

## License

MIT License - See LICENSE file for details.
