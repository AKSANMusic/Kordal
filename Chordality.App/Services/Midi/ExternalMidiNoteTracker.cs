using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Windows.Devices.Midi;

namespace Chordality.App.Services.Midi;

public class ExternalMidiNoteTracker
{
    private IMidiOutPort? _midiOutPort;
    private readonly object _lock = new();

    // Track active notes per channel to prevent stuck notes on hardware
    // Key: (Channel << 8) | Note
    private readonly HashSet<int> _activeNotes = new();

    public void SetPort(IMidiOutPort? port)
    {
        lock (_lock)
        {
            if (_midiOutPort != null)
            {
                // Send all notes off to previous device before disconnecting
                SendAllNotesOffToPort(_midiOutPort);
                _activeNotes.Clear();
            }
            _midiOutPort = port;
        }
    }

    public void NoteOn(byte channel, byte note, byte velocity)
    {
        lock (_lock)
        {
            if (_midiOutPort == null) return;

            int key = (channel << 8) | note;

            // CRITICAL: If note is already playing, send NoteOff first
            if (_activeNotes.Contains(key))
            {
                _midiOutPort.SendMessage(new MidiNoteOffMessage(channel, note, 0));
                _activeNotes.Remove(key);
            }

            _midiOutPort.SendMessage(new MidiNoteOnMessage(channel, note, velocity));
            _activeNotes.Add(key);
        }
    }

    public void NoteOff(byte channel, byte note)
    {
        lock (_lock)
        {
            if (_midiOutPort == null) return;

            int key = (channel << 8) | note;

            if (_activeNotes.Contains(key))
            {
                _midiOutPort.SendMessage(new MidiNoteOffMessage(channel, note, 0));
                _activeNotes.Remove(key);
            }
        }
    }

    public void AllNotesOff(byte channel)
    {
        lock (_lock)
        {
            if (_midiOutPort == null) return;

            var keysToRemove = new List<int>();

            foreach (var key in _activeNotes)
            {
                if ((key >> 8) == channel)
                {
                    byte note = (byte)(key & 0xFF);
                    _midiOutPort.SendMessage(new MidiNoteOffMessage(channel, note, 0));
                    keysToRemove.Add(key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _activeNotes.Remove(key);
            }

            // Also send standard MIDI Control Change 123 (All Notes Off) as a fallback
            _midiOutPort.SendMessage(new MidiControlChangeMessage(channel, 123, 0));
        }
    }

    private void SendAllNotesOffToPort(IMidiOutPort port)
    {
        // Fire CC 123 on all 16 channels just to be absolutely certain
        for (byte i = 0; i < 16; i++)
        {
            port.SendMessage(new MidiControlChangeMessage(i, 123, 0));
        }
    }
}
