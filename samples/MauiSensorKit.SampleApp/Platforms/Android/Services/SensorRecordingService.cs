using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using MauiSensorKit;
using Microsoft.Extensions.Logging;
using Android.Provider;

namespace MauiSensorKit.SampleApp.Platforms.Android.Services;

[Service(
    Name = "com.zenithcodestudio.sampleapp.SensorRecordingService",
    ForegroundServiceType = ForegroundService.TypeDataSync | ForegroundService.TypeLocation,
    Permission = "android.permission.FOREGROUND_SERVICE",
    Exported = false)]
public class SensorRecordingService : Service
{
    private const string ChannelId = "SensorRecordingChannel";
    private const int NotificationId = 1001;
    private const string ActionStart = "com.zenithcodestudio.sampleapp.action.START_RECORDING";
    private const string ActionStop = "com.zenithcodestudio.sampleapp.action.STOP_RECORDING";

    private ISensorCollectionService? _sensorService;
    private ILogger<SensorRecordingService>? _logger;
    private bool _isRunning;
    private PowerManager.WakeLock? _wakeLock;

    public override void OnCreate()
    {
        base.OnCreate();
        
        try
        {
            var services = IPlatformApplication.Current?.Services;
            if (services != null)
            {
                _sensorService = services.GetService<ISensorCollectionService>();
                _logger = services.GetService<ILogger<SensorRecordingService>>();
            }
        }
        catch (Exception ex)
        {
            global::Android.Util.Log.Error("SensorRecordingService", $"Error initializing service: {ex.Message}");
        }
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action;
        
        _logger?.LogInformation("Service action received: {Action}", action ?? "null");

        if (action == ActionStop)
        {
            StopRecording();
            return StartCommandResult.NotSticky;
        }

        if (!_isRunning)
        {
            StartRecording();
        }

        return StartCommandResult.Sticky;
    }

    private void StartRecording()
    {
        try
        {
            // Acquire wake lock to keep CPU running
            AcquireWakeLock();
            
            // Disable battery optimizations for this app
            DisableBatteryOptimization();
            
            CreateNotificationChannel();
            var notification = CreateNotification();
            
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                StartForeground(NotificationId, notification, ForegroundService.TypeDataSync | ForegroundService.TypeLocation);
            }
            else
            {
                StartForeground(NotificationId, notification);
            }

            _sensorService?.StartAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger?.LogError(task.Exception, "Error starting sensor service");
                }
            });

            _isRunning = true;
            _logger?.LogInformation("Sensor recording service started with wake lock");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error starting recording service");
        }
    }

    private void AcquireWakeLock()
    {
        try
        {
            var powerManager = GetSystemService(PowerService) as PowerManager;
            if (powerManager != null)
            {
                _wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "MauiSensorKit::RecordingWakeLock");
                _wakeLock?.SetReferenceCounted(false);
                _wakeLock?.Acquire();
                _logger?.LogInformation("Wake lock acquired for background recording");
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error acquiring wake lock");
        }
    }

    private void ReleaseWakeLock()
    {
        try
        {
            _wakeLock?.Release();
            _wakeLock = null;
            _logger?.LogInformation("Wake lock released");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error releasing wake lock");
        }
    }

    private void DisableBatteryOptimization()
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(23))
            {
                var powerManager = GetSystemService(PowerService) as PowerManager;
                var packageName = PackageName;
                
                if (powerManager?.IsIgnoringBatteryOptimizations(packageName) == false)
                {
                    var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
                    intent.SetData(global::Android.Net.Uri.Parse("package:" + packageName));
                    intent.AddFlags(ActivityFlags.NewTask);
                    StartActivity(intent);
                    _logger?.LogInformation("Requested battery optimization exemption");
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error disabling battery optimization");
        }
    }

    private void StopRecording()
    {
        try
        {
            _sensorService?.StopAsync().ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger?.LogError(task.Exception, "Error stopping sensor service");
                }
            });

            _isRunning = false;
            _logger?.LogInformation("Sensor recording service stopped");
            
            ReleaseWakeLock();
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error stopping recording service");
        }
    }

    public override IBinder? OnBind(Intent? intent) => null;

    private void CreateNotificationChannel()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(
                ChannelId,
                GetString(Resource.String.channel_name) ?? "Sensor Recording",
                NotificationImportance.Low)
            {
                Description = GetString(Resource.String.channel_description) ?? "Background sensor data collection"
            };

            var notificationManager = GetSystemService(NotificationService) as NotificationManager;
            notificationManager?.CreateNotificationChannel(channel);
        }
    }

    private Notification CreateNotification()
    {
        var context = global::Android.App.Application.Context;
        
        var intent = new Intent(context, typeof(MainActivity));
        intent.SetFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        intent.SetAction(Intent.ActionMain);
        intent.AddCategory(Intent.CategoryLauncher);

        var pendingIntent = PendingIntent.GetActivity(
            context,
            0,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle(GetString(Resource.String.notification_title) ?? "Recording sensor data")
            .SetContentText(GetString(Resource.String.notification_text) ?? "Sensor data is being collected")
            .SetSmallIcon(Resource.Drawable.notification_icon)
            .SetOngoing(true)
            .SetContentIntent(pendingIntent)
            .SetPriority(NotificationCompat.PriorityLow);

        return builder.Build();
    }

    public static void StartService(Context context)
    {
        var intent = new Intent(context, typeof(SensorRecordingService));
        intent.SetAction(ActionStart);
        
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
    }

    public static void StopService(Context context)
    {
        var intent = new Intent(context, typeof(SensorRecordingService));
        intent.SetAction(ActionStop);
        context.StartService(intent);
    }
}
