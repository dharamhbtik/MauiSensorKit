namespace MauiSensorKit;

/// <summary>
/// Specifies the type of NFC tag detected.
/// </summary>
public enum NfcTagType
{
    /// <summary>
    /// NDEF formatted tag with readable records.
    /// </summary>
    Ndef,

    /// <summary>
    /// ISO-DEP (ISO 14443-4) compliant tag.
    /// </summary>
    IsoDep,

    /// <summary>
    /// MIFARE Classic tag.
    /// </summary>
    MifareClassic,

    /// <summary>
    /// MIFARE Ultralight tag.
    /// </summary>
    MifareUltralight,

    /// <summary>
    /// NFC Forum Type 1 tag.
    /// </summary>
    NfcF,

    /// <summary>
    /// NFC Forum Type 2 tag.
    /// </summary>
    NfcA,

    /// <summary>
    /// NFC Forum Type 3 tag.
    /// </summary>
    NfcB,

    /// <summary>
    /// NFC Forum Type 4 tag.
    /// </summary>
    NfcV,

    /// <summary>
    /// Unknown or unrecognized tag type.
    /// </summary>
    Unknown
}
