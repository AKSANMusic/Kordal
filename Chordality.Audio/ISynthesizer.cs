using System;

namespace Chordality.Audio;

public interface ISynthesizer
{
    void NoteOn(int midiNote, float velocity = 1.0f);
    void NoteOff(int midiNote);
    void AllNotesOff();

    event Action<int, float>? OnNoteOnFired;
    event Action<int>? OnNoteOffFired;
}
