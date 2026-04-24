using Microsoft.Extensions.Logging;

namespace MauiSensorKit;

/// <summary>
/// Service for crash reporting via Firebase Crashlytics.
/// This service gracefully handles cases where Firebase is not configured (no google-services.json).
/// </summary>
public sealed class FirebaseCrashlyticsService
{
    private readonly ILogger<FirebaseCrashlyticsService> _logger;
    private bool _isInitialized;
    private bool _isFirebaseAvailable;

    /// <summary>
    /// Initializes a new instance of the <see cref="FirebaseCrashlyticsService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public FirebaseCrashlyticsService(ILogger<FirebaseCrashlyticsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a value indicating whether Firebase Crashlytics is available and initialized.
    /// </summary>
    public bool IsAvailable => _isFirebaseAvailable;

    /// <summary>
    /// Attempts to initialize Firebase Crashlytics.
    /// If google-services.json is not present, Firebase will not be initialized but the app will continue to work.
    /// </summary>
    public void TryInitialize()
    {
        if (_isInitialized)
            return;

        try
        {
#if ANDROID
            // Check if Firebase is configured by trying to access the app
            var firebaseApps = Firebase.FirebaseApp.GetApps(global::Android.App.Application.Context);
            if (firebaseApps != null && firebaseApps.Count > 0)
            {
                _isFirebaseAvailable = true;
                _logger.LogInformation("Firebase Crashlytics initialized successfully");
            }
            else
            {
                _logger.LogWarning("Firebase not configured (no google-services.json found). Crashlytics will be disabled.");
            }
#elif IOS
            // On iOS, Firebase initialization is handled via GoogleService-Info.plist
            if (Firebase.Crashlytics.Crashlytics.SharedInstance != null)
            {
                _isFirebaseAvailable = true;
                _logger.LogInformation("Firebase Crashlytics initialized successfully");
            }
            else
            {
                _logger.LogWarning("Firebase not configured (no GoogleService-Info.plist found). Crashlytics will be disabled.");
            }
#else
            _logger.LogInformation("Firebase Crashlytics not supported on this platform");
#endif
        }
        catch (Exception ex)
        {
            // Firebase is not configured - this is expected if google-services.json is missing
            _logger.LogWarning("Firebase Crashlytics not available: {Message}. This is normal if google-services.json is not configured.", ex.Message);
            _isFirebaseAvailable = false;
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Records an exception in Firebase Crashlytics if available.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    /// <param name="context">Optional context information.</param>
    public void RecordException(Exception exception, string? context = null)
    {
        if (!_isFirebaseAvailable)
        {
            _logger.LogDebug("Firebase Crashlytics not available, logging exception locally: {Message}", exception.Message);
            return;
        }

        try
        {
#if ANDROID || IOS
            // Use reflection to call Firebase Crashlytics
            var crashlyticsType = Type.GetType("Firebase.Crashlytics.FirebaseCrashlytics, Plugin.Firebase.Crashlytics");
            if (crashlyticsType != null)
            {
                var instanceProperty = crashlyticsType.GetProperty("Instance");
                var recordExceptionMethod = crashlyticsType.GetMethod("RecordException", new[] { typeof(Exception) });
                
                if (instanceProperty != null && recordExceptionMethod != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    
                    // Add context as a custom key if provided
                    if (!string.IsNullOrEmpty(context))
                    {
                        var setCustomKeyMethod = crashlyticsType.GetMethod("SetCustomKey", new[] { typeof(string), typeof(string) });
                        setCustomKeyMethod?.Invoke(instance, new object[] { "context", context });
                    }
                    
                    recordExceptionMethod.Invoke(instance, new object[] { exception });
                    _logger.LogDebug("Recorded exception in Firebase Crashlytics: {Message}", exception.Message);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording exception in Firebase Crashlytics");
        }
    }

    /// <summary>
    /// Logs a message to Firebase Crashlytics if available.
    /// </summary>
    /// <param name="message">The message to log.</param>
    public void Log(string message)
    {
        if (!_isFirebaseAvailable)
        {
            _logger.LogDebug("Firebase Crashlytics not available, logging message locally: {Message}", message);
            return;
        }

        try
        {
#if ANDROID || IOS
            var crashlyticsType = Type.GetType("Firebase.Crashlytics.FirebaseCrashlytics, Plugin.Firebase.Crashlytics");
            if (crashlyticsType != null)
            {
                var instanceProperty = crashlyticsType.GetProperty("Instance");
                var logMethod = crashlyticsType.GetMethod("Log", new[] { typeof(string) });
                
                if (instanceProperty != null && logMethod != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    logMethod.Invoke(instance, new object[] { message });
                }
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging message to Firebase Crashlytics");
        }
    }

    /// <summary>
    /// Sets a custom key-value pair in Firebase Crashlytics if available.
    /// </summary>
    /// <param name="key">The custom key.</param>
    /// <param name="value">The value.</param>
    public void SetCustomKey(string key, string value)
    {
        if (!_isFirebaseAvailable)
        {
            _logger.LogDebug("Firebase Crashlytics not available, skipping custom key: {Key}", key);
            return;
        }

        try
        {
#if ANDROID || IOS
            var crashlyticsType = Type.GetType("Firebase.Crashlytics.FirebaseCrashlytics, Plugin.Firebase.Crashlytics");
            if (crashlyticsType != null)
            {
                var instanceProperty = crashlyticsType.GetProperty("Instance");
                var setCustomKeyMethod = crashlyticsType.GetMethod("SetCustomKey", new[] { typeof(string), typeof(string) });
                
                if (instanceProperty != null && setCustomKeyMethod != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    setCustomKeyMethod.Invoke(instance, new object[] { key, value });
                    _logger.LogDebug("Set Firebase Crashlytics custom key: {Key}", key);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting custom key in Firebase Crashlytics: {Key}", key);
        }
    }

    /// <summary>
    /// Sets the user ID in Firebase Crashlytics if available.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    public void SetUserId(string userId)
    {
        if (!_isFirebaseAvailable)
        {
            _logger.LogDebug("Firebase Crashlytics not available, skipping user ID");
            return;
        }

        try
        {
#if ANDROID || IOS
            var crashlyticsType = Type.GetType("Firebase.Crashlytics.FirebaseCrashlytics, Plugin.Firebase.Crashlytics");
            if (crashlyticsType != null)
            {
                var instanceProperty = crashlyticsType.GetProperty("Instance");
                var setUserIdMethod = crashlyticsType.GetMethod("SetUserId", new[] { typeof(string) });
                
                if (instanceProperty != null && setUserIdMethod != null)
                {
                    var instance = instanceProperty.GetValue(null);
                    setUserIdMethod.Invoke(instance, new object[] { userId });
                    _logger.LogDebug("Set Firebase Crashlytics user ID: {UserId}", userId);
                }
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting Firebase Crashlytics user ID");
        }
    }

    /// <summary>
    /// Forces a crash test (for debugging purposes only).
    /// </summary>
    public void ForceCrash()
    {
        if (!_isFirebaseAvailable)
        {
            _logger.LogWarning("Firebase Crashlytics not available, cannot force crash");
            return;
        }

        try
        {
#if ANDROID || IOS
            var crashlyticsType = Type.GetType("Firebase.Crashlytics.FirebaseCrashlytics, Plugin.Firebase.Crashlytics");
            if (crashlyticsType != null)
            {
                var crashMethod = crashlyticsType.GetMethod("Crash");
                crashMethod?.Invoke(null, null);
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error forcing crash");
        }
    }
}
