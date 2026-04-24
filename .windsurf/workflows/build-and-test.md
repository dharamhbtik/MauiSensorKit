---
description: Build and test the MauiSensorKit library
---

# Build and Test MauiSensorKit

## Prerequisites
- .NET 10 SDK
- MAUI workload installed
- Android SDK (for Android builds)
- Xcode (for iOS builds)

## Build the Library

1. Restore packages:
   ```bash
   cd /Users/dkumar/CascadeProjects/MauiSensorKit/src/MauiSensorKit
   dotnet restore
   ```

2. Build the library:
   ```bash
   // turbo
   dotnet build -c Release
   ```

## Build the Sample App

1. Restore packages:
   ```bash
   cd /Users/dkumar/CascadeProjects/MauiSensorKit/samples/MauiSensorKit.SampleApp
   dotnet restore
   ```

2. Build for Android:
   ```bash
   // turbo
   dotnet build -f net10.0-android -c Release
   ```

3. Build for iOS:
   ```bash
   // turbo
   dotnet build -f net10.0-ios -c Release
   ```

## Create NuGet Package

1. Pack the library:
   ```bash
   cd /Users/dkumar/CascadeProjects/MauiSensorKit/src/MauiSensorKit
   // turbo
   dotnet pack -c Release
   ```

2. The package will be in `bin/Release/` folder.

## Run Sample App

### Android
```bash
cd /Users/dkumar/CascadeProjects/MauiSensorKit/samples/MauiSensorKit.SampleApp
// turbo
dotnet build -t:Run -f net10.0-android
```

### iOS Simulator
```bash
cd /Users/dkumar/CascadeProjects/MauiSensorKit/samples/MauiSensorKit.SampleApp
// turbo
dotnet build -t:Run -f net10.0-ios -p:_DeviceName=:v2:runtime=com.apple.CoreSimulator.SimRuntime.iOS-17-0,device=com.apple.CoreSimulator.SimDeviceType.iPhone-15
```
