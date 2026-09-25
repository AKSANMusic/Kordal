using System;
using System.Collections.Generic;
using System.Linq;

namespace Chordality.Engine;

/// <summary>
/// Specifies the base triad quality.
/// </summary>
public enum BaseQuality
{
    Major,
    Minor
}

/// <summary>
/// Represents the exact extensions and modifiers present in the UI modifier matrix.
/// </summary>
public enum ChordModifier
{
    Maj7,
    Dom7,    // 7
    Six,     // 6
    Sus2,
    Sus4,
    Nine,    // 9
    Eleven,  // 11
    Thirteen,// 13
    Flat5,   // b5
    Sharp5,  // #5
    Flat9,   // b9
    Sharp9,  // #9
    Sharp11, // #11
    Flat13,  // b13
    Dim,
    Aug
}

/// <summary>
/// Logic for generating chord intervals based on mathematical offsets.
/// </summary>
public static class Harmony
{
    /// <summary>
    /// Calculates the integer offsets (intervals in semitones) from the root
    /// for a given base quality and set of modifiers.
    /// </summary>
    public static int[] GetIntervals(BaseQuality baseQuality, IEnumerable<ChordModifier> modifiers)
    {
        var intervals = new HashSet<int> { 0 }; // Root

        int third = baseQuality == BaseQuality.Minor ? 3 : 4;
        int fifth = 7;

        bool omitThird = false;
        bool omitFifth = false;

        var addIntervals = new HashSet<int>();

        if (modifiers != null)
        {
            foreach (var mod in modifiers)
            {
                switch (mod)
                {
                    case ChordModifier.Sus2:
                        omitThird = true;
                        addIntervals.Add(2);
                        break;
                    case ChordModifier.Sus4:
                        omitThird = true;
                        addIntervals.Add(5);
                        break;
                    case ChordModifier.Dim:
                        third = 3;
                        fifth = 6;
                        break;
                    case ChordModifier.Aug:
                        fifth = 8;
                        break;
                    case ChordModifier.Flat5:
                        fifth = 6;
                        break;
                    case ChordModifier.Sharp5:
                        fifth = 8;
                        break;
                    case ChordModifier.Maj7:
                        addIntervals.Add(11);
                        break;
                    case ChordModifier.Dom7:
                        addIntervals.Add(10);
                        break;
                    case ChordModifier.Six:
                        addIntervals.Add(9);
                        break;
                    case ChordModifier.Nine:
                        addIntervals.Add(14); // 1 octave + 2 semitones
                        break;
                    case ChordModifier.Eleven:
                        addIntervals.Add(17); // 1 octave + 5 semitones
                        break;
                    case ChordModifier.Thirteen:
                        addIntervals.Add(21); // 1 octave + 9 semitones
                        break;
                    case ChordModifier.Flat9:
                        addIntervals.Add(13);
                        break;
                    case ChordModifier.Sharp9:
                        addIntervals.Add(15);
                        break;
                    case ChordModifier.Sharp11:
                        addIntervals.Add(18);
                        break;
                    case ChordModifier.Flat13:
                        addIntervals.Add(20);
                        break;
                }
            }
        }

        if (!omitThird) intervals.Add(third);
        if (!omitFifth) intervals.Add(fifth);

        foreach (var add in addIntervals)
        {
            intervals.Add(add);
        }

        var result = intervals.ToList();
        result.Sort(); // Ensure intervals are sorted from lowest to highest
        return result.ToArray();
    }
}
