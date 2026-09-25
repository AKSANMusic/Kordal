using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace Chordality.Audio;

public class PolyphonicSynthesizer : ISynthesizer, ISampleProvider
{
    public WaveFormat WaveFormat { get; }

    private readonly PolyphonicVoice[] _voices;
    private readonly int _maxVoices;
    private readonly float[] _mixBuffer;

    // Track active notes strictly
    private readonly HashSet<int> _activeNotes = new();
    private readonly object _lock = new();

    public event Action<int, float>? OnNoteOnFired;
    public event Action<int>? OnNoteOffFired;

    public PolyphonicSynthesizer(int sampleRate = 44100, int maxVoices = 16)
    {
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
        _maxVoices = maxVoices;
        _voices = new PolyphonicVoice[maxVoices];
        for (int i = 0; i < maxVoices; i++)
        {
            _voices[i] = new PolyphonicVoice(sampleRate);
        }

        // Internal mixing buffer to prevent allocating on audio thread
        _mixBuffer = new float[4096];
    }

    public void NoteOn(int midiNote, float velocity = 1.0f)
    {
        lock (_lock)
        {
            if (_activeNotes.Contains(midiNote))
            {
                // Re-trigger if already playing
                NoteOff(midiNote);
            }

            // Find free voice
            PolyphonicVoice? freeVoice = null;
            foreach (var voice in _voices)
            {
                if (!voice.IsActive)
                {
                    freeVoice = voice;
                    break;
                }
            }

            // Simple voice stealing if all full: steal the first one
            if (freeVoice == null)
            {
                freeVoice = _voices[0];
            }

            freeVoice.NoteOn(midiNote, velocity);
            _activeNotes.Add(midiNote);
            OnNoteOnFired?.Invoke(midiNote, velocity);
        }
    }

    public void NoteOff(int midiNote)
    {
        lock (_lock)
        {
            if (_activeNotes.Contains(midiNote))
            {
                foreach (var voice in _voices)
                {
                    if (voice.CurrentMidiNote == midiNote && voice.IsActive)
                    {
                        voice.NoteOff(midiNote);
                    }
                }
                _activeNotes.Remove(midiNote);
                OnNoteOffFired?.Invoke(midiNote);
            }
        }
    }

    public void AllNotesOff()
    {
        lock (_lock)
        {
            foreach (var voice in _voices)
            {
                voice.Kill();
            }
            _activeNotes.Clear();
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        // Internal mix buffer handling is strictly pre-allocated.
        // Assuming typical NAudio chunks are small enough, but guard just in case.
        float[] mixBuffer = _mixBuffer;
        if (mixBuffer.Length < count)
        {
            mixBuffer = new float[count];
            // Since we lock during execution, it is safe to assign back (though not thread-safe to re-size dynamically without locks).
            // For this phase, we'll just let it allocate locally if it ever exceeds 4096 (which it shouldn't at 50ms buffer).
        }

        Array.Clear(buffer, offset, count);

        lock (_lock)
        {
            foreach (var voice in _voices)
            {
                if (voice.IsActive)
                {
                    Array.Clear(mixBuffer, 0, count);
                    int read = voice.Read(mixBuffer, 0, count);

                    // Mix into main buffer
                    for (int i = 0; i < read; i++)
                    {
                        buffer[offset + i] += mixBuffer[i];
                    }
                }
            }
        }

        return count;
    }
}
