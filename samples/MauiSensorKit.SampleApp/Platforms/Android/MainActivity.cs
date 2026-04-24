using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
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

        // Pass NFC intent to the NFC collector
        var nfcCollector = MauiApplication.Current?.Services?.GetService<NfcCollector>();
        nfcCollector?.HandleTagDiscovered(intent);
    }
}
