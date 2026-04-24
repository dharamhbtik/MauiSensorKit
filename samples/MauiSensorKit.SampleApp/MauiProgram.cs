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
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // Register ViewModels
        builder.Services.AddTransient<SensorSelectionViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();

        // Register Views
        builder.Services.AddTransient<SensorSelectionPage>();
        builder.Services.AddTransient<DashboardPage>();

        return builder.Build();
    }
}

public class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
        MainPage = new AppShell(services);
    }
}

public class AppShell : Shell
{
    public AppShell(IServiceProvider services)
    {
        Routing.RegisterRoute("sensorselection", typeof(SensorSelectionPage));
        Routing.RegisterRoute("dashboard", typeof(DashboardPage));

        Items.Add(new FlyoutItem
        {
            Title = "Configure Sensors",
            Icon = new FontImageSource { Glyph = "\ue8b8", FontFamily = "MaterialIcons", Size = 20 },
            Items =
            {
                new ShellContent
                {
                    Title = "Configure",
                    ContentTemplate = new DataTemplate(() => services.GetRequiredService<SensorSelectionPage>())
                }
            }
        });

        Items.Add(new FlyoutItem
        {
            Title = "Dashboard",
            Icon = new FontImageSource { Glyph = "\ue871", FontFamily = "MaterialIcons", Size = 20 },
            Items =
            {
                new ShellContent
                {
                    Title = "Dashboard",
                    ContentTemplate = new DataTemplate(() => services.GetRequiredService<DashboardPage>())
                }
            }
        });

        // Set initial route
        CurrentItem = Items[0];
    }
}
