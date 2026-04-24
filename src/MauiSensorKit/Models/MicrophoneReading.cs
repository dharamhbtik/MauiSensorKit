namespace MauiSensorKit;

/// <summary>
/// Represents a microphone sensor reading measuring audio amplitude/sound level.
/// Note: This only stores amplitude data, NOT raw PCM audio.
/// </summary>
public sealed record MicrophoneReading : SensorReading
{
    /// <summary>
    /// Gets the type of sensor.
    /// </summary>
    public override SensorType Type => SensorType.Microphone;

    /// <summary>
    /// Gets the sound level in decibels.
    /// </summary>
    public double AmplitudeDb { get; init; }

    /// <summary>
    /// Gets the peak amplitude measured during the sample window.
    /// </summary>
    public double PeakAmplitude { get; init; }

    /// <summary>
    /// Gets a description of the sound level based on decibel value.
    /// </summary>
    public string SoundLevelDescription => AmplitudeDb switch
    {
        < 30 => "Very Quiet",
        < 50 => "Quiet",
        < 70 => "Normal Conversation",
        < 85 => "Loud",
        < 100 => "Very Loud",
        _ => "Extremely Loud"
    };

    /// <summary>
    /// Returns a formatted string representation of the reading.
    /// </summary>
    public override string ToString()
    {
        return $"Microphone: {AmplitudeDb:F1} dB ({SoundLevelDescription})";
    }
}
