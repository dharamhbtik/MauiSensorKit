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

        // Request all permissions
        var results = await Permissions.RequestMultipleAsync(permissions.ToArray());

        // Check if critical permissions were granted
        var locationGranted = results.TryGetValue(Manifest.Permission.AccessFineLocation, out var locationStatus) &&
                              locationStatus == PermissionStatus.Granted;

        return locationGranted;
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
