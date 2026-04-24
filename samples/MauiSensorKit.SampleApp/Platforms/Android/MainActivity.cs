using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Maui.ApplicationModel;
using MauiSensorKit;
using MauiSensorKit.SampleApp.Views;

namespace MauiSensorKit.SampleApp;

[Activity(Name = "com.zenithcodestudio.sampleapp.MainActivity", Theme = "@style/Maui.SplashTheme", MainLauncher = true, Exported = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Android.Content.Intent.ActionMain }, Categories = new[] { Android.Content.Intent.CategoryLauncher })]
[IntentFilter(new[] { "android.nfc.action.NDEF_DISCOVERED" }, Categories = new[] { Android.Content.Intent.CategoryDefault }, DataMimeType = "text/plain")]
[IntentFilter(new[] { "android.nfc.action.TAG_DISCOVERED" }, Categories = new[] { Android.Content.Intent.CategoryDefault })]
[IntentFilter(new[] { "android.nfc.action.TECH_DISCOVERED" })]
[MetaData("android.nfc.action.TECH_DISCOVERED", Resource = "@xml/nfc_tech_filter")]
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
