#if ANDROID
using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Java.Nio.Charset;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for NFC tag detection events.
/// Note: NFC is event-driven, not continuous. The collector listens passively for tag detection.
/// </summary>
public sealed class NfcCollector : BaseSensorCollector<NfcCollector>
{
    private string? _sessionId;

#if ANDROID
    private NfcAdapter? _nfcAdapter;
    private BroadcastReceiver? _receiver;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="NfcCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public NfcCollector(ILogger<NfcCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Nfc;

    /// <inheritdoc/>
    public override Task<bool> IsSupportedAsync()
    {
#if ANDROID
        try
        {
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(global::Android.App.Application.Context);
            return Task.FromResult(_nfcAdapter?.IsEnabled == true);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking NFC support on Android");
            return Task.FromResult(false);
        }
#elif IOS
        // iOS supports NFC on iPhone 7 and later
        Logger.LogInformation("iOS NFC support requires CoreNFC framework and user interaction");
        return Task.FromResult(true);
#else
        Logger.LogWarning("NFC not supported on this platform");
        return Task.FromResult(false);
#endif
    }

    /// <inheritdoc/>
    public override Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("NFC collector is already running");
            return Task.CompletedTask;
        }

        try
        {
            _sessionId = sessionId;

#if ANDROID
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(global::Android.App.Application.Context);
            if (_nfcAdapter?.IsEnabled != true)
            {
                Logger.LogWarning("NFC is not available or enabled on this Android device");
                return Task.CompletedTask;
            }

            // Note: In a real app, you would need to use PendingIntent and handle
            // tag detection in the Activity's OnNewIntent method. This is a simplified
            // collector that demonstrates the concept.

            Logger.LogInformation("NFC collector started. Note: Tag detection requires proper Intent handling in Activity.");

#elif IOS
            Logger.LogInformation("iOS NFC: CoreNFC requires active tag reading sessions. This collector provides passive detection only.");

            // On iOS, emit a simulated reading since we can't do passive listening
            var reading = new NfcReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                TagId = "simulated",
                TagType = NfcTagType.Unknown,
                IsSimulated = true
            };
            RaiseReading(reading);

            Logger.LogInformation("iOS NFC: For actual tag reading, implement NFCTagReaderSession.");

            return Task.CompletedTask;
#else
            Logger.LogWarning("NFC not supported on this platform");
            return Task.CompletedTask;
#endif

            IsRunning = true;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting NFC collector");
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public override Task StopAsync()
    {
        if (!IsRunning)
        {
            return Task.CompletedTask;
        }

        try
        {
#if ANDROID
            // Cleanup would happen here
#endif

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("NFC collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping NFC collector");
        }

        return Task.CompletedTask;
    }

#if ANDROID
    /// <summary>
    /// This method should be called from the Activity's OnNewIntent when an NFC tag is detected.
    /// </summary>
    public void HandleTagDiscovered(Intent intent)
    {
        if (intent.Action != NfcAdapter.ActionTagDiscovered &&
            intent.Action != NfcAdapter.ActionNdefDiscovered &&
            intent.Action != NfcAdapter.ActionTechDiscovered)
        {
            return;
        }

        try
        {
            var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag == null) return;

            var tagId = BitConverter.ToString(tag.GetId()).Replace("-", "");
            var tagType = DetectTagType(tag);
            string? ndefMessage = null;

            // Try to read NDEF data
            if (Ndef.Get(tag) is Ndef ndef && ndef.IsConnected)
            {
                try
                {
                    var ndefMessageObj = ndef.NdefMessage;
                    if (ndefMessageObj != null)
                    {
                        var records = ndefMessageObj.GetRecords();
                        if (records != null && records.Length > 0)
                        {
                            var payloads = records
                                .Where(r => r != null)
                                .Select(r => System.Text.Encoding.UTF8.GetString(r.GetPayload()))
                                .ToList();
                            ndefMessage = System.Text.Json.JsonSerializer.Serialize(payloads);
                        }
                    }
                }
                finally
                {
                    ndef.Close();
                }
            }

            var reading = new NfcReading
            {
                DeviceId = DeviceId,
                SessionId = _sessionId ?? string.Empty,
                TagId = tagId,
                TagType = tagType,
                NdefMessage = ndefMessage,
                IsSimulated = false
            };

            RaiseReading(reading);
            Logger.LogInformation("NFC tag detected: {TagId} ({TagType})", tagId, tagType);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error processing NFC tag");
        }
    }

    private NfcTagType DetectTagType(Tag tag)
    {
        var techList = tag.GetTechList();
        if (techList == null) return NfcTagType.Unknown;

        if (techList.Contains("android.nfc.tech.Ndef"))
            return NfcTagType.Ndef;
        if (techList.Contains("android.nfc.tech.IsoDep"))
            return NfcTagType.IsoDep;
        if (techList.Contains("android.nfc.tech.MifareClassic"))
            return NfcTagType.MifareClassic;
        if (techList.Contains("android.nfc.tech.MifareUltralight"))
            return NfcTagType.MifareUltralight;
        if (techList.Contains("android.nfc.tech.NfcA"))
            return NfcTagType.NfcA;
        if (techList.Contains("android.nfc.tech.NfcB"))
            return NfcTagType.NfcB;
        if (techList.Contains("android.nfc.tech.NfcF"))
            return NfcTagType.NfcF;
        if (techList.Contains("android.nfc.tech.NfcV"))
            return NfcTagType.NfcV;

        return NfcTagType.Unknown;
    }
#endif
}
