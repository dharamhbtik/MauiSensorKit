namespace MauiSensorKit;

/// <summary>
/// Helper class for checking network connectivity conditions.
/// </summary>
public sealed class ConnectivityHelper
{
    private readonly ILogger<ConnectivityHelper> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectivityHelper"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ConnectivityHelper(ILogger<ConnectivityHelper> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a value indicating whether the device has any network connectivity.
    /// </summary>
    public bool IsConnected => Connectivity.Current?.NetworkAccess == NetworkAccess.Internet;

    /// <summary>
    /// Gets a value indicating whether the device is connected via WiFi.
    /// </summary>
    public bool IsWifiConnected
    {
        get
        {
            var profiles = Connectivity.Current?.ConnectionProfiles;
            return profiles?.Contains(ConnectionProfile.WiFi) ?? false;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the device is connected via cellular.
    /// </summary>
    public bool IsCellularConnected
    {
        get
        {
            var profiles = Connectivity.Current?.ConnectionProfiles;
            return profiles?.Contains(ConnectionProfile.Cellular) ?? false;
        }
    }

    /// <summary>
    /// Checks if upload conditions are met based on connectivity requirements.
    /// </summary>
    /// <param name="requireWifi">Whether WiFi is required for upload.</param>
    /// <returns>True if upload can proceed; otherwise, false.</returns>
    public bool CanUpload(bool requireWifi)
    {
        if (!IsConnected)
        {
            _logger.LogDebug("Cannot upload: No internet connectivity");
            return false;
        }

        if (requireWifi && !IsWifiConnected)
        {
            _logger.LogDebug("Cannot upload: WiFi required but not connected");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a description of the current connectivity state.
    /// </summary>
    public string GetConnectivityDescription()
    {
        var access = Connectivity.Current?.NetworkAccess.ToString() ?? "Unknown";
        var profiles = Connectivity.Current?.ConnectionProfiles;
        var profileList = profiles != null ? string.Join(", ", profiles) : "None";

        return $"Network Access: {access}, Profiles: {profileList}";
    }
}
