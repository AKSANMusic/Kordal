using System;
using NAudio.Wave;

namespace Chordality.Audio;

public class PolyphonicVoice : ISampleProvider
{
    public WaveFormat WaveFormat { get; }

    private readonly double _sampleRate;
    private double _phase;
    private double _frequency;
    private double _phaseIncrement;
    private float _velocity;

    // Envelope states
    private enum EnvelopeState { Idle, Attack, Decay, Sustain, Release }
    private EnvelopeState _envelopeState = EnvelopeState.Idle;

    // Envelope parameters (in seconds)
    private const double AttackTime = 0.01;  // Fast attack to prevent clicking but keep punch
    private const double DecayTime = 0.1;
    private const double SustainLevel = 0.8;
    private const double ReleaseTime = 0.1;  // Fast release to prevent ringing

    private double _envelopeValue;
    private double _envelopeIncrement;

    public int CurrentMidiNote { get; private set; } = -1;
    public bool IsActive => _envelopeState != EnvelopeState.Idle;

    public PolyphonicVoice(int sampleRate)
    {
        _sampleRate = sampleRate;
        WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
    }

    public void NoteOn(int midiNote, float velocity)
    {
        CurrentMidiNote = midiNote;
        _velocity = velocity;

        // Calculate frequency from MIDI note: f = 440 * 2^((d-69)/12)
        _frequency = 440.0 * Math.Pow(2.0, (midiNote - 69.0) / 12.0);
        _phaseIncrement = _frequency / _sampleRate;

        // Start envelope
        _envelopeState = EnvelopeState.Attack;
        _envelopeValue = 0.0;
        _envelopeIncrement = 1.0 / (AttackTime * _sampleRate);
    }

    public void NoteOff(int midiNote)
    {
        if (CurrentMidiNote == midiNote && _envelopeState != EnvelopeState.Idle)
        {
            _envelopeState = EnvelopeState.Release;
            _envelopeIncrement = -_envelopeValue / (ReleaseTime * _sampleRate);
        }
    }

    public void Kill()
    {
        // Smooth release rather than hard snap to prevent clicking
        if (_envelopeState != EnvelopeState.Idle)
        {
            _envelopeState = EnvelopeState.Release;
            _envelopeIncrement = -_envelopeValue / (0.01 * _sampleRate); // Fast 10ms fade out
        }
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (_envelopeState == EnvelopeState.Idle)
        {
            Array.Clear(buffer, offset, count);
            return count;
        }

        for (int i = 0; i < count; i++)
        {
            // Update envelope
            switch (_envelopeState)
            {
                case EnvelopeState.Attack:
                    _envelopeValue += _envelopeIncrement;
                    if (_envelopeValue >= 1.0)
                    {
                        _envelopeValue = 1.0;
                        _envelopeState = EnvelopeState.Decay;
                        _envelopeIncrement = -(1.0 - SustainLevel) / (DecayTime * _sampleRate);
                    }
                    break;
                case EnvelopeState.Decay:
                    _envelopeValue += _envelopeIncrement;
                    if (_envelopeValue <= SustainLevel)
                    {
                        _envelopeValue = SustainLevel;
                        _envelopeState = EnvelopeState.Sustain;
                        _envelopeIncrement = 0;
                    }
                    break;
                case EnvelopeState.Release:
                    _envelopeValue += _envelopeIncrement;
                    if (_envelopeValue <= 0.0)
                    {
                        _envelopeValue = 0.0;
                        _envelopeState = EnvelopeState.Idle;
                        CurrentMidiNote = -1;
                    }
                    break;
            }

            // Generate Sawtooth wave: 2 * (phase - floor(phase + 0.5))
            double sample = 2.0 * (_phase - Math.Floor(_phase + 0.5));

            // Apply volume, envelope, and a slight low-pass inherent to synth mix reduction
            buffer[offset + i] = (float)(sample * _envelopeValue * _velocity * 0.2); // 0.2 volume headroom

            _phase += _phaseIncrement;
            if (_phase > 1.0) _phase -= 1.0;
        }

        return count;
    }
}
