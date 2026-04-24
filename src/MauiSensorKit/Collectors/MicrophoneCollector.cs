#if ANDROID
using Android.Media;
#endif

#if IOS
using AVFoundation;
using Foundation;
#endif

namespace MauiSensorKit;

/// <summary>
/// Collector for microphone amplitude data (sound level in dB, not raw audio).
/// </summary>
public sealed class MicrophoneCollector : BaseSensorCollector<MicrophoneCollector>
{
    private string? _sessionId;
    private CancellationTokenSource? _cancellationTokenSource;

#if ANDROID
    private AudioRecord? _audioRecord;
    private const int SampleRate = 44100;
    private const int BufferSize = 1024;
#endif

#if IOS
    private AVAudioRecorder? _recorder;
    private NSUrl? _tempUrl;
#endif

    /// <summary>
    /// Initializes a new instance of the <see cref="MicrophoneCollector"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="options">The sensor kit options.</param>
    public MicrophoneCollector(ILogger<MicrophoneCollector> logger, SensorKitOptions options)
        : base(logger, options)
    {
    }

    /// <inheritdoc/>
    public override SensorType SensorType => SensorType.Microphone;

    /// <inheritdoc/>
    public override async Task<bool> IsSupportedAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
            return status == PermissionStatus.Granted || status == PermissionStatus.Unknown;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error checking microphone support");
            return false;
        }
    }

    /// <inheritdoc/>
    public override async Task StartAsync(string sessionId, CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            Logger.LogWarning("Microphone collector is already running");
            return;
        }

        try
        {
            // Request microphone permission first
            var status = await Permissions.RequestAsync<Permissions.Microphone>();
            if (status != PermissionStatus.Granted)
            {
                Logger.LogWarning("Microphone permission not granted");
                return;
            }

            _sessionId = sessionId;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

#if ANDROID
            // Initialize Android AudioRecord
            var minBufferSize = AudioRecord.GetMinBufferSize(SampleRate, ChannelIn.Mono, Android.Media.Encoding.Pcm16bit);
            _audioRecord = new AudioRecord(
                AudioSource.Mic,
                SampleRate,
                ChannelIn.Mono,
                Android.Media.Encoding.Pcm16bit,
                Math.Max(BufferSize, minBufferSize));

            if (_audioRecord.State != State.Initialized)
            {
                Logger.LogError("Failed to initialize AudioRecord");
                return;
            }

            _audioRecord.StartRecording();

            // Start amplitude polling loop
            _ = Task.Run(async () =>
            {
                var buffer = new short[BufferSize];
                double peakAmplitude = 0;

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        int read = _audioRecord.Read(buffer, 0, buffer.Length);
                        if (read > 0)
                        {
                            // Calculate RMS amplitude
                            double sum = 0;
                            double max = 0;
                            for (int i = 0; i < read; i++)
                            {
                                double sample = buffer[i] / 32768.0; // Normalize to [-1, 1]
                                sum += sample * sample;
                                max = Math.Max(max, Math.Abs(sample));
                            }
                            double rms = Math.Sqrt(sum / read);
                            peakAmplitude = Math.Max(peakAmplitude, max);

                            // Convert to dB (full scale reference)
                            double db = rms > 0 ? 20 * Math.Log10(rms) : -100;
                            double peakDb = peakAmplitude > 0 ? 20 * Math.Log10(peakAmplitude) : -100;

                            // Clamp to reasonable range
                            db = Math.Max(db, -100);
                            peakDb = Math.Max(peakDb, -100);

                            var reading = new MicrophoneReading
                            {
                                DeviceId = DeviceId,
                                SessionId = _sessionId ?? string.Empty,
                                AmplitudeDb = db,
                                PeakAmplitude = peakDb,
                                IsSimulated = false
                            };

                            RaiseReading(reading);
                        }

                        await Task.Delay(Options.MicrophonePollingInterval, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error processing microphone audio");
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                    }
                }
            }, _cancellationTokenSource.Token);

#elif IOS
            // iOS: Use AVAudioRecorder with metering
            string tempFile = Path.Combine(Path.GetTempPath(), "temp_audio.caf");
            _tempUrl = NSUrl.FromFilename(tempFile);

            var settings = new AudioSettings
            {
                SampleRate = 44100,
                NumberChannels = 1,
                Format = AudioToolbox.AudioFormatType.LinearPCM,
                LinearPcmBitDepth = 16,
                LinearPcmFloatKey = false,
                LinearPcmBigEndianKey = false
            };

            _recorder = AVAudioRecorder.Create(_tempUrl, settings, out NSError? error);
            if (error != null)
            {
                Logger.LogError("Failed to create AVAudioRecorder: {Error}", error.LocalizedDescription);
                return;
            }

            _recorder.MeteringEnabled = true;
            _recorder.Record();

            // Start amplitude polling loop
            _ = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        _recorder.UpdateMeters();
                        float averagePower = _recorder.AveragePower(0);
                        float peakPower = _recorder.PeakPower(0);

                        var reading = new MicrophoneReading
                        {
                            DeviceId = DeviceId,
                            SessionId = _sessionId ?? string.Empty,
                            AmplitudeDb = averagePower,
                            PeakAmplitude = peakPower,
                            IsSimulated = false
                        };

                        RaiseReading(reading);

                        await Task.Delay(Options.MicrophonePollingInterval, _cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error processing microphone audio");
                        await Task.Delay(1000, _cancellationTokenSource.Token);
                    }
                }
            }, _cancellationTokenSource.Token);

#else
            Logger.LogWarning("Microphone not supported on this platform");
            return;
#endif

            IsRunning = true;
            Logger.LogInformation("Microphone collector started with interval {Interval}", Options.MicrophonePollingInterval);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error starting microphone collector");
            throw;
        }
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
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

#if ANDROID
            _audioRecord?.Stop();
            _audioRecord?.Release();
            _audioRecord?.Dispose();
            _audioRecord = null;
#elif IOS
            _recorder?.Stop();
            _recorder?.Dispose();
            _recorder = null;

            // Clean up temp file
            if (_tempUrl != null && NSFileManager.DefaultManager.FileExists(_tempUrl.Path))
            {
                NSFileManager.DefaultManager.Remove(_tempUrl, out _);
            }
            _tempUrl = null;
#endif

            IsRunning = false;
            _sessionId = null;

            Logger.LogInformation("Microphone collector stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error stopping microphone collector");
        }

        return Task.CompletedTask;
    }
}
