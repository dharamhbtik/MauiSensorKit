using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using MauiSensorKit;
using MauiSensorKit.SampleApp.Views;
using MauiSensorKit.SampleApp.Platforms.Android.Helpers;
using MauiSensorKit.SampleApp.Platforms.Android.Services;

namespace MauiSensorKit.SampleApp;

[Activity(Name = "com.zenithcodestudio.sampleapp.MainActivity", Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { Android.Content.Intent.CategoryLauncher })]
[IntentFilter(new[] { "android.nfc.action.NDEF_DISCOVERED" }, Categories = new[] { Android.Content.Intent.CategoryDefault }, DataMimeType = "text/plain")]
[IntentFilter(new[] { "android.nfc.action.TAG_DISCOVERED" }, Categories = new[] { Android.Content.Intent.CategoryDefault })]
[IntentFilter(new[] { "android.nfc.action.TECH_DISCOVERED" })]
[MetaData("android.nfc.action.TECH_DISCOVERED", Resource = "@xml/nfc_tech_filter")]
public class MainActivity : MauiAppCompatActivity
{
    private bool _permissionsRequested = false;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Handle NFC intent if started via NFC tag
        HandleNfcIntent(Intent);
    }

    protected override async void OnResume()
    {
        base.OnResume();

        // Request permissions on first resume (after MAUI is initialized)
        if (!_permissionsRequested)
        {
            _permissionsRequested = true;
            await RequestPermissionsAsync();
        }
    }

    protected override void OnPause()
    {
        base.OnPause();
        // Ensure foreground service notification is shown when app goes to background
        // This follows Android guidelines for background processing
        EnsureForegroundServiceRunning();
    }

    private void EnsureForegroundServiceRunning()
    {
        try
        {
            // Check if sensor service is recording
            var app = IPlatformApplication.Current;
            if (app?.Services != null)
            {
                var sensorService = app.Services.GetService<ISensorCollectionService>();
                if (sensorService?.IsRunning == true)
                {
                    // Service should already be running with notification
                    // This ensures the notification stays visible when app is in background
                    System.Diagnostics.Debug.WriteLine("App going to background - foreground service notification active");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking foreground service: {ex.Message}");
        }
    }

    private async Task RequestPermissionsAsync()
    {
        try
        {
            // Request essential permissions
            var granted = await PermissionHelper.RequestEssentialPermissionsAsync();

            if (!granted)
            {
                System.Diagnostics.Debug.WriteLine("Critical permissions not granted");
            }

            // Request background location (for background sensor recording)
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                await PermissionHelper.RequestBackgroundLocationPermissionAsync();
            }

            // Request high sampling rate permission (Android 12+)
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                await PermissionHelper.RequestHighSamplingRatePermissionAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error requesting permissions: {ex.Message}");
        }
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        HandleNfcIntent(intent);
    }

    private void HandleNfcIntent(Intent? intent)
    {
        if (intent == null) return;

        try
        {
            // Pass NFC intent to the NFC collector
            var app = IPlatformApplication.Current;
            if (app?.Services != null)
            {
                var nfcCollector = app.Services.GetService<NfcCollector>();
                nfcCollector?.HandleTagDiscovered(intent);
            }
        }
        catch (Exception ex)
        {
            // Log but don't crash if NFC handling fails
            System.Diagnostics.Debug.WriteLine($"Error handling NFC intent: {ex.Message}");
        }
    }
}
