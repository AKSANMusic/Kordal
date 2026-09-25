using System;
using System.Collections.Concurrent;
using System.Linq;
using NAudio.Wave;

namespace Chordality.Audio;

public enum PlaybackMode
{
    Chord, // Simultaneous notes
    Strum, // Slight delay between notes
    Arp    // Tempo-synced loop
}

public class SequencingSampleProvider : ISampleProvider
{
    private readonly PolyphonicSynthesizer _synth;
    public WaveFormat WaveFormat => _synth.WaveFormat;

    private readonly int _sampleRate;
    private long _currentSample;

    // State
    private volatile PlaybackMode _mode = PlaybackMode.Chord;
    private double _bpm = 120.0;

    // Notes currently driven by the sequencer
    private int[] _targetNotes = Array.Empty<int>();
    private readonly object _lock = new();

    // Arpeggiator / Strum State
    private int _arpIndex;
    private long _nextEventSample;
    private bool _strumFinished;
    private int[] _currentlyPlayingNotes = Array.Empty<int>();

    // Thread-safe command queue from UI
    private readonly ConcurrentQueue<Action> _commandQueue = new();

    public SequencingSampleProvider(PolyphonicSynthesizer synth)
    {
        _synth = synth;
        _sampleRate = synth.WaveFormat.SampleRate;
    }

    public void SetMode(PlaybackMode mode)
    {
        _commandQueue.Enqueue(() =>
        {
            if (_mode != mode)
            {
                _synth.AllNotesOff();
                _mode = mode;
                ResetSequenceState();
                TriggerSequence();
            }
        });
    }

    public void SetBpm(double bpm)
    {
        _commandQueue.Enqueue(() =>
        {
            _bpm = bpm;
        });
    }

    public void UpdateNotes(int[] notes)
    {
        // Must copy array for thread safety
        int[] newNotes = (int[])notes.Clone();

        _commandQueue.Enqueue(() =>
        {
            _targetNotes = newNotes;

            if (_mode == PlaybackMode.Chord)
            {
                // Immediate diffing for Polyphonic Chord mode
                foreach (var note in _currentlyPlayingNotes)
                {
                    if (!Array.Exists(_targetNotes, n => n == note))
                    {
                        _synth.NoteOff(note);
                    }
                }
                foreach (var note in _targetNotes)
                {
                    if (!Array.Exists(_currentlyPlayingNotes, n => n == note))
                    {
                        _synth.NoteOn(note, 0.8f);
                    }
                }
                _currentlyPlayingNotes = _targetNotes;
            }
            else
            {
                // For Strum/Arp, restart sequence on chord change
                _synth.AllNotesOff();
                _currentlyPlayingNotes = Array.Empty<int>();
                ResetSequenceState();
                TriggerSequence();
            }
        });
    }

    private void ResetSequenceState()
    {
        _arpIndex = 0;
        _strumFinished = false;
        _nextEventSample = _currentSample; // Trigger immediately
    }

    private void TriggerSequence()
    {
        if (_targetNotes.Length == 0) return;

        if (_mode == PlaybackMode.Strum)
        {
            if (_arpIndex < _targetNotes.Length)
            {
                _synth.NoteOn(_targetNotes[_arpIndex], 0.8f);
                _arpIndex++;

                // 25ms delay per note for strum
                long delaySamples = (long)(0.025 * _sampleRate);
                _nextEventSample = _currentSample + delaySamples;
            }
            else
            {
                _strumFinished = true;
            }
        }
        else if (_mode == PlaybackMode.Arp)
        {
            _synth.AllNotesOff(); // Staccato arp
            _synth.NoteOn(_targetNotes[_arpIndex], 0.8f);

            _arpIndex++;
            if (_arpIndex >= _targetNotes.Length) _arpIndex = 0;

            // 16th note calculation:
            // Beats per second = BPM / 60
            // 16th notes per second = (BPM / 60) * 4
            // Samples per 16th note = SampleRate / 16th notes per second
            double sixteenthNotesPerSecond = (_bpm / 60.0) * 4.0;
            long samplesPerSixteenth = (long)(_sampleRate / sixteenthNotesPerSecond);

            _nextEventSample = _currentSample + samplesPerSixteenth;
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        // 1. Process pending UI commands before filling buffer
        while (_commandQueue.TryDequeue(out var command))
        {
            command();
        }

        // 2. Sample-accurate sub-block rendering
        int samplesRendered = 0;
        while (samplesRendered < count)
        {
            int samplesToRenderThisPass = count - samplesRendered;

            // Do we have a sequenced event pending inside this chunk?
            if ((_mode == PlaybackMode.Strum && !_strumFinished) || _mode == PlaybackMode.Arp)
            {
                long samplesUntilEvent = _nextEventSample - _currentSample;

                if (samplesUntilEvent <= 0)
                {
                    // Fire event exactly now
                    TriggerSequence();
                    // Recalculate samples until next event
                    samplesUntilEvent = _nextEventSample - _currentSample;
                }

                if (samplesUntilEvent > 0 && samplesUntilEvent < samplesToRenderThisPass)
                {
                    // Render exactly up to the event boundary
                    samplesToRenderThisPass = (int)samplesUntilEvent;
                }
            }

            // Fill this sub-block from the synth
            int read = _synth.Read(buffer, offset + samplesRendered, samplesToRenderThisPass);
            if (read == 0) break; // Should never happen unless synth stops

            _currentSample += read;
            samplesRendered += read;
        }

        return samplesRendered;
    }
}
