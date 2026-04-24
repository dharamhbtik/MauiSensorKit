using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using MauiSensorKit;
using MauiSensorKit.SampleApp.Views;

namespace MauiSensorKit.SampleApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Handle NFC intent if started via NFC tag
        // Delay to ensure MAUI is initialized
        HandleNfcIntent(Intent);
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
            // Use IPlatformApplication.Current.Services instead of obsolete MauiApplication.Current.Services
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
