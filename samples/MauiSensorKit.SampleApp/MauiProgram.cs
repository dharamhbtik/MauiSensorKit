using Microsoft.Extensions.Logging;
using MauiSensorKit;
using MauiSensorKit.SampleApp.ViewModels;
using MauiSensorKit.SampleApp.Views;
using MauiSensorKit.SampleApp.Services;
using Microcharts.Maui;
using CommunityToolkit.Maui;

namespace MauiSensorKit.SampleApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .UseMauiCommunityToolkit()
            .UseMicrocharts()
            .UseMauiSensorKit(
                options =>
                {
                    // Load enabled sensors from preferences, or enable all by default
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
                    else
                    {
                        // Enable all sensors by default if no preferences set
                        options.EnableAll();
                    }

                    // Configure intervals
                    options.LocationInterval = TimeSpan.FromSeconds(5);
                    options.BatteryPollingInterval = TimeSpan.FromSeconds(60); // 1 minute for graph
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

        // Register data stores for route and battery tracking
        builder.Services.AddSingleton<RouteDataStore>();
        builder.Services.AddSingleton<BatteryDataStore>();
        builder.Services.AddSingleton<BackgroundDataStoreConnector>();
        
        // Register session state service for cross-page data sharing
        builder.Services.AddSingleton<SessionStateService>();

        // Register ViewModels - Singletons persist state across navigation
        builder.Services.AddTransient<SensorSelectionViewModel>();
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddSingleton<ActivityRecognitionViewModel>();
        builder.Services.AddSingleton<RouteTrackerViewModel>();
        builder.Services.AddSingleton<BatteryGraphViewModel>();
        builder.Services.AddSingleton<MotionSensorsViewModel>();
        builder.Services.AddSingleton<BatteryViewModel>();
        builder.Services.AddSingleton<MapViewModel>();

        // Register Views
        builder.Services.AddTransient<SensorSelectionPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ActivityRecognitionPage>();
        builder.Services.AddTransient<RouteTrackerPage>();
        builder.Services.AddTransient<BatteryGraphPage>();
        builder.Services.AddTransient<MotionSensorsPage>();
        builder.Services.AddTransient<BatteryPage>();
        builder.Services.AddTransient<MapPage>();

        return builder.Build();
    }
}
