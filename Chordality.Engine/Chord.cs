using System;
using System.Collections.Generic;
using System.Linq;

namespace Chordality.Engine;

/// <summary>
/// Handles chord generation into absolute MIDI notes.
/// </summary>
public static class Chord
{
    /// <summary>
    /// Generates an array of absolute MIDI notes for a given root, octave, base quality, modifiers, and inversion state.
    /// Positive inversions transpose lowest notes up an octave (+12).
    /// Negative inversions transpose highest notes down an octave (-12).
    /// </summary>
    public static int[] GenerateNotes(PitchClass root, int octave, BaseQuality baseQuality, IEnumerable<ChordModifier> modifiers, int inversion = 0)
    {
        int rootMidiNote = Pitch.GetMidiNote(root, octave);
        int[] intervals = Harmony.GetIntervals(baseQuality, modifiers);

        var notes = new List<int>(intervals.Length);
        for (int i = 0; i < intervals.Length; i++)
        {
            notes.Add(rootMidiNote + intervals[i]);
        }

        if (inversion > 0)
        {
            // Positive inversion: move the lowest note up an octave (num of times = inversion)
            for (int i = 0; i < inversion; i++)
            {
                int minIndex = 0;
                int minNote = notes[0];
                for (int j = 1; j < notes.Count; j++)
                {
                    if (notes[j] < minNote)
                    {
                        minNote = notes[j];
                        minIndex = j;
                    }
                }
                notes[minIndex] += 12;
            }
        }
        else if (inversion < 0)
        {
            // Negative inversion: move the highest note down an octave
            int absInv = Math.Abs(inversion);
            for (int i = 0; i < absInv; i++)
            {
                int maxIndex = 0;
                int maxNote = notes[0];
                for (int j = 1; j < notes.Count; j++)
                {
                    if (notes[j] > maxNote)
                    {
                        maxNote = notes[j];
                        maxIndex = j;
                    }
                }
                notes[maxIndex] -= 12;
            }
        }

        notes.Sort();

        for (int i = 0; i < notes.Count; i++)
        {
            notes[i] = Pitch.ClampMidiNote(notes[i]);
        }

        // Return unique sorted notes in case clamp squashed notes together, though typically we just clamp them
        return notes.Distinct().ToArray();
    }
}
