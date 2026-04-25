using Android;
using Android.Content.PM;
using Android.OS;

namespace MauiSensorKit.SampleApp.Platforms.Android.Helpers;

public static class PermissionHelper
{
    public static async Task<bool> RequestEssentialPermissionsAsync()
    {
        var permissions = new List<string>();

        // Location permissions (essential for most sensor use cases)
        permissions.Add(Manifest.Permission.AccessFineLocation);
        permissions.Add(Manifest.Permission.AccessCoarseLocation);

        // Motion sensors (Android 10+)
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            permissions.Add(Manifest.Permission.ActivityRecognition);
        }

        // Body sensors (heart rate, etc)
        permissions.Add(Manifest.Permission.BodySensors);

        // Audio recording
        permissions.Add(Manifest.Permission.RecordAudio);

        // NFC
        permissions.Add(Manifest.Permission.Nfc);

        // UWB (Android 12+)
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            permissions.Add(Manifest.Permission.UwbRanging);
        }

        // Notification permission (Android 13+)
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            permissions.Add(Manifest.Permission.PostNotifications);
        }

        // Request permissions individually
        var locationStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        await Permissions.RequestAsync<Permissions.Microphone>();
        
        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            await Permissions.RequestAsync<Permissions.Sensors>();
        }
        
        // Post notification permission (Android 13+) handled separately
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            await Permissions.RequestAsync<PostNotificationPermission>();
        }

        return locationStatus == PermissionStatus.Granted;
    }

    public static async Task<bool> RequestBackgroundLocationPermissionAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            return true;

        var status = await Permissions.RequestAsync<Permissions.LocationAlways>();
        return status == PermissionStatus.Granted;
    }

    public static async Task<bool> RequestHighSamplingRatePermissionAsync()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            return true;

        var result = await Permissions.RequestAsync<HighSamplingRatePermission>();
        return result == PermissionStatus.Granted;
    }
}

public class HighSamplingRatePermission : Permissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        new[] { (Manifest.Permission.HighSamplingRateSensors, true) };
}
