using System.Linq;

namespace Chordality.App.ViewModels;

public static class PitchFormatter
{
    private static readonly string[] PitchNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    public static string GetNoteName(int midiNote)
    {
        int octave = (midiNote / 12) - 1;
        int pitchClass = midiNote % 12;
        return $"{PitchNames[pitchClass]}{octave}";
    }

    public static int GetPitchClass(int midiNote)
    {
        return midiNote % 12;
    }
}
