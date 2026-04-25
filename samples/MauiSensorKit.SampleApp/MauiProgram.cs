using Microsoft.Extensions.Logging;
using MauiSensorKit;
using MauiSensorKit.SampleApp.ViewModels;
using MauiSensorKit.SampleApp.Views;

namespace MauiSensorKit.SampleApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiSensorKit(
                options =>
                {
                    // Load enabled sensors from preferences
                    var enabledJson = Preferences.Default.Get<string?>("MauiSensorKit_EnabledSensors", null);
                    if (!string.IsNullOrEmpty(enabledJson))
                    {
                        try
                        {
                            var enabled = System.Text.Json.JsonSerializer.Deserialize<Dictionary<SensorType, bool>>(enabledJson);
                            if (enabled != null)
                            {
                                options.EnabledSensors = enabled;
                            }
                        }
                        catch { }
                    }

                    // Configure intervals
                    options.LocationInterval = TimeSpan.FromSeconds(5);
                    options.BatteryPollingInterval = TimeSpan.FromSeconds(30);
                    options.MicrophonePollingInterval = TimeSpan.FromSeconds(1);
                    options.SlowSensorPollingInterval = TimeSpan.FromSeconds(10);
                    options.BatchSize = 100;
                    options.BatchFlushInterval = TimeSpan.FromSeconds(30);
                },
                upload =>
                {
                    upload.EnableUpload = Preferences.Default.Get("MauiSensorKit_UploadEnabled", false);
                    upload.ApiEndpointUrl = Preferences.Default.Get<string?>("MauiSensorKit_UploadUrl", null);
                    
                    var apiKey = Preferences.Default.Get<string?>("MauiSensorKit_ApiKey", null);
                    if (!string.IsNullOrEmpty(apiKey))
                    {
                        upload.Headers["Authorization"] = $"Bearer {apiKey}";
                    }
                    
                    upload.UploadOnlyOnWifi = Preferences.Default.Get("MauiSensorKit_UploadOnlyOnWifi", false);
                    upload.DeleteAfterUpload = true;
                    upload.MaxRetryAttempts = 3;
                    upload.UploadRetryInterval = TimeSpan.FromMinutes(1);
                })
;
        // Note: Using default MAUI fonts. Add custom fonts to Resources/Fonts and register them here if needed.

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register ViewModels
        builder.Services.AddTransient<SensorSelectionViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ActivityRecognitionViewModel>();

        // Register Views
        builder.Services.AddTransient<SensorSelectionPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ActivityRecognitionPage>();

        return builder.Build();
    }
}
