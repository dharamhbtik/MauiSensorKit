namespace MauiSensorKit;

/// <summary>
/// Represents an NFC tag detection event.
/// </summary>
public sealed record NfcReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Nfc;

    /// <summary>
    /// Gets the unique identifier of the NFC tag as a hex string.
    /// </summary>
    public string TagId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of NFC tag detected.
    /// </summary>
    public NfcTagType TagType { get; init; } = NfcTagType.Unknown;

    /// <summary>
    /// Gets the JSON-encoded NDEF message if the tag is readable and contains NDEF data.
    /// </summary>
    public string? NdefMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether the tag contains readable NDEF data.
    /// </summary>
    public bool HasNdefData => !string.IsNullOrEmpty(NdefMessage);

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        var ndef = HasNdefData ? " [NDEF]" : "";
        return $"NFC Tag: {TagId} ({TagType}){ndef}";
    }
}
