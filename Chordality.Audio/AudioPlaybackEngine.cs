using System;
using System.Diagnostics;
using NAudio.Wave;

namespace Chordality.Audio;

public class AudioPlaybackEngine : IDisposable
{
    private WasapiOut? _outputDevice;
    public PolyphonicSynthesizer Synthesizer { get; }

    public AudioPlaybackEngine(int sampleRate = 44100, int maxVoices = 16)
    {
        Synthesizer = new PolyphonicSynthesizer(sampleRate, maxVoices);
        InitializeAudio();
    }

    private void InitializeAudio()
    {
        try
        {
            // Use WASAPI in Shared Mode for low latency and system compatibility
            _outputDevice = new WasapiOut(NAudio.CoreAudioApi.AudioClientShareMode.Shared, 50);
            _outputDevice.Init(Synthesizer);
            _outputDevice.Play();

            // Subscribe to stopped event for naive recovery loop
            _outputDevice.PlaybackStopped += OutputDevice_PlaybackStopped;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to initialize audio device: {ex.Message}");
            // Failsafe state: if no audio device, the UI won't crash
        }
    }

    private void OutputDevice_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // Simple recovery loop for device loss (e.g. headphones unplugged)
        // If there was an error, we try to restart
        if (e.Exception != null)
        {
            Debug.WriteLine($"Audio playback stopped due to error: {e.Exception.Message}. Attempting recovery...");
            RecoverAudio();
        }
    }

    private void RecoverAudio()
    {
        DisposeOutputDevice();
        try
        {
            // Simple sleep to let OS device tree settle before re-initializing
            System.Threading.Thread.Sleep(500);
            InitializeAudio();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Recovery failed: {ex.Message}");
        }
    }

    private void DisposeOutputDevice()
    {
        if (_outputDevice != null)
        {
            _outputDevice.PlaybackStopped -= OutputDevice_PlaybackStopped;
            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
        }
    }

    public void Dispose()
    {
        Synthesizer.AllNotesOff();
        DisposeOutputDevice();
    }
}
