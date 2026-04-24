using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Service for tracking analytics events via Firebase Analytics.
/// This service gracefully handles cases where Firebase is not configured (no google-services.json).
/// </summary>
public sealed class FirebaseAnalyticsService
{
    private readonly ILogger<FirebaseAnalyticsService> _logger;
    private bool _isInitialized;
    private bool _isFirebaseAvailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirebaseAnalyticsService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FirebaseAnalyticsService(ILogger<FirebaseAnalyticsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a value indicating whether Firebase Analytics is available and initialized.
    /// </summary>
    public bool IsAvailable => _isFirebaseAvailable;

    /// <summary>
    /// Attempts to initialize Firebase Analytics.
    /// If google-services.json is not present, Firebase will not be initialized but the app will continue to work.
    /// </summary>
    public void TryInitialize()
    {
        if (_isInitialized)
            return;

        try
        {
#if ANDROID
            // Check if Firebase is configured using reflection
            var firebaseAppType = Type.GetType("Firebase.FirebaseApp, Plugin.Firebase.Analytics");
            if (firebaseAppType != null)
            {
                var getAppsMethod = firebaseAppType.GetMethod("GetApps", new[] { typeof(Android.Content.Context) });
                if (getAppsMethod != null)
                {
                    var context = Android.App.Application.Context;
                    var apps = getAppsMethod.Invoke(null, new object[] { context }) as System.Collections.IList;
                    
                    if (apps != null && apps.Count > 0)
                    {
                        _isFirebaseAvailable = true;
                        _logger.LogInformation("Firebase Analytics initialized successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Firebase not configured (no google-services.json found). Analytics will be disabled.");
                    }
                }
            }
            else
            {
                _logger.LogWarning("Firebase SDK not found in project. Analytics will be disabled.");
            }
#elif IOS
            // Check if Firebase is configured using reflection
            var appType = Type.GetType("Firebase.Core.App, Plugin.Firebase.Analytics");
            if (appType != null)
            {
                var defaultInstanceProperty = appType.GetProperty("DefaultInstance");
                if (defaultInstanceProperty != null)
                {
                    var defaultInstance = defaultInstanceProperty.GetValue(null);
                    if (defaultInstance != null)
                    {
                        _isFirebaseAvailable = true;
                        _logger.LogInformation("Firebase Analytics initialized successfully");
                    }
                    else
                    {
                        _logger.LogWarning("Firebase not configured (no GoogleService-Info.plist found). Analytics will be disabled.");
                    }
                }
            }
            else
            {
                _logger.LogWarning("Firebase SDK not found in project. Analytics will be disabled.");
            }
#else
            _logger.LogInformation("Firebase Analytics not supported on this platform");
#endif
        }
        catch (Exception ex)
        {
            // Firebase is not configured - this is expected if google-services.json is missing
            _logger.LogWarning("Firebase Analytics not available: {Message}. This is normal if google-services.json is not configured.", ex.Message);
            _isFirebaseAvailable = false;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Logs an event to Firebase Analytics if available.
    /// </summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="parameters">Optional event parameters.</param>
    public void LogEvent(string eventName, Dictionary<string, string>? parameters = null)
    {
        if (!_isFirebaseAvailable)
        {
            _logger.LogDebug("Firebase Analytics not available, skipping event: {EventName}", eventName);
            return;
        }

        try
        {
#if ANDROID
            var bundle = new Android.OS.Bundle();
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    bundle.PutString(param.Key, param.Value);
                }
            }
            
            // Use reflection to call Firebase Analytics since we don't know if it's available at compile time
            var analyticsType = Type.GetType("Firebase.Analytics.FirebaseAnalytics, Plugin.Firebase.Analytics");
            if (analyticsType != null)
            {
                var instanceProperty = analyticsType.GetProperty("Instance");
                var logEventMethod = analyticsType.GetMethod("LogEvent", new[] { typeof(string), typeof(Android.OS.Bundle) });
                
                if (instanceProperty != null && logEventMethod != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    logEventMethod.Invoke(instance, new object[] { eventName, bundle });
                    _logger.LogDebug("Logged Firebase Analytics event: {EventName}", eventName);
                }
            }
#elif IOS
            // Use reflection for iOS Firebase Analytics
            var analyticsType = Type.GetType("Firebase.Analytics.Analytics, Plugin.Firebase.Analytics");
            if (analyticsType != null)
            {
                var logEventMethod = analyticsType.GetMethod("LogEvent", new[] { typeof(string), typeof(Foundation.NSDictionary) });
                if (logEventMethod != null)
                {
                    var nsDict = new Foundation.NSMutableDictionary();
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            nsDict[param.Key] = new Foundation.NSString(param.Value);
                        }
                    }
                    
                    logEventMethod.Invoke(null, new object[] { eventName, nsDict });
                    _logger.LogDebug("Logged Firebase Analytics event: {EventName}", eventName);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging Firebase Analytics event: {EventName}", eventName);
        }
    }

    /// <summary>
    /// Sets a user property in Firebase Analytics if available.
    /// </summary>
    /// <param name="name">The property name.</param>
    /// <param name="value">The property value.</param>
    public void SetUserProperty(string name, string value)
    {
        if (!_isFirebaseAvailable)
        {
            _logger.LogDebug("Firebase Analytics not available, skipping user property: {Name}", name);
            return;
        }

        try
        {
#if ANDROID || IOS
            var analyticsType = Type.GetType("Firebase.Analytics.FirebaseAnalytics, Plugin.Firebase.Analytics");
            if (analyticsType != null)
            {
                var setUserPropertyMethod = analyticsType.GetMethod("SetUserProperty", new[] { typeof(string), typeof(string) });
                if (setUserPropertyMethod != null)
                {
                    setUserPropertyMethod.Invoke(null, new object[] { name, value });
                    _logger.LogDebug("Set Firebase Analytics user property: {Name}", name);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Firebase Analytics user property: {Name}", name);
        }
    }

    /// <summary>
    /// Sets the user ID in Firebase Analytics if available.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    public void SetUserId(string userId)
    {
        if (!_isFirebaseAvailable)
        {
            _logger.LogDebug("Firebase Analytics not available, skipping user ID");
            return;
        }

        try
        {
#if ANDROID || IOS
            var analyticsType = Type.GetType("Firebase.Analytics.FirebaseAnalytics, Plugin.Firebase.Analytics");
            if (analyticsType != null)
            {
                var setUserIdMethod = analyticsType.GetMethod("SetUserId", new[] { typeof(string) });
                if (setUserIdMethod != null)
                {
                    setUserIdMethod.Invoke(null, new object[] { userId });
                    _logger.LogDebug("Set Firebase Analytics user ID: {UserId}", userId);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Firebase Analytics user ID");
        }
    }
}
