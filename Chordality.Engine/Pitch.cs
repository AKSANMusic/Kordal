namespace Chordality.Engine;

/// <summary>
/// Represents a pitch class (C=0 to B=11)
/// </summary>
public enum PitchClass
{
    C = 0,
    CSharp = 1,
    D = 2,
    DSharp = 3,
    E = 4,
    F = 5,
    FSharp = 6,
    G = 7,
    GSharp = 8,
    A = 9,
    ASharp = 10,
    B = 11
}

/// <summary>
/// Utility methods for pitch calculations.
/// </summary>
public static class Pitch
{
    public const int MinMidiNote = 0;
    public const int MaxMidiNote = 127;

    /// <summary>
    /// Calculates the absolute MIDI note number for a given pitch class and octave.
    /// Assumes C4 = 60, meaning Octave 4 is the middle octave.
    /// Octave -1 for C is MIDI note 0.
    /// </summary>
    public static int GetMidiNote(PitchClass pitchClass, int octave)
    {
        // C4 = 60 => C0 = 12, C-1 = 0
        int note = ((octave + 1) * 12) + (int)pitchClass;
        return note;
    }

    /// <summary>
    /// Clamps a MIDI note to the valid range (0-127).
    /// </summary>
    public static int ClampMidiNote(int note)
    {
        if (note < MinMidiNote) return MinMidiNote;
        if (note > MaxMidiNote) return MaxMidiNote;
        return note;
    }
}
