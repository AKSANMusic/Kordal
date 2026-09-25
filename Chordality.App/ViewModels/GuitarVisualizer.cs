using System;
using System.Collections.Generic;
using System.Linq;
using Chordality.Engine;

namespace Chordality.App.ViewModels;

public static class GuitarVisualizer
{
    // Standard tuning absolute MIDI notes
    // E2=40, A2=45, D3=50, G3=55, B3=59, E4=64
    public static readonly int[] StandardTuning = { 40, 45, 50, 55, 59, 64 };
    public const int MaxFrets = 15;

    public class FretPosition
    {
        public int StringIndex { get; set; } // 0 (Low E) to 5 (High E)
        public int Fret { get; set; }        // 0 (Open) to 15
        public int MidiNote { get; set; }
    }

    /// <summary>
    /// Calculates a human-playable guitar voicing for the given absolute active notes.
    /// Prioritizes open strings and bounds the stretch to a maximum of 5 frets.
    /// </summary>
    public static List<FretPosition> CalculateFingering(int[] activeNotes)
    {
        var result = new List<FretPosition>();
        if (activeNotes == null || activeNotes.Length == 0) return result;

        // Note: For a real algorithm, we'd do a complex recursive backtracking search
        // evaluating chord voicings based on distance cost and open string bonuses.
        // For Phase 2, we implement a greedy algorithm mapping the absolute notes from lowest to highest.

        // Target active notes
        var targetNotes = new List<int>(activeNotes);

        // Track which strings are used
        var usedStrings = new HashSet<int>();

        int minFret = -1;
        int maxFret = -1;

        // Simple greedy mapping from lowest note to highest
        foreach (var note in targetNotes)
        {
            FretPosition bestPos = null;

            // Try to find an open string first
            for (int s = 0; s < 6; s++)
            {
                if (usedStrings.Contains(s)) continue;

                if (StandardTuning[s] == note)
                {
                    bestPos = new FretPosition { StringIndex = s, Fret = 0, MidiNote = note };
                    break;
                }
            }

            // If no open string, find the most ergonomic fret position
            if (bestPos == null)
            {
                for (int s = 0; s < 6; s++)
                {
                    if (usedStrings.Contains(s)) continue;

                    int fret = note - StandardTuning[s];
                    if (fret > 0 && fret <= MaxFrets)
                    {
                        // Check fret span constraints
                        bool validSpan = true;
                        if (minFret != -1 && maxFret != -1)
                        {
                            int newMin = Math.Min(minFret, fret);
                            int newMax = Math.Max(maxFret, fret);
                            if (newMax - newMin > 4) // Max span of 4 frets (5 total)
                            {
                                validSpan = false;
                            }
                        }

                        if (validSpan)
                        {
                            bestPos = new FretPosition { StringIndex = s, Fret = fret, MidiNote = note };
                            break;
                        }
                    }
                }
            }

            if (bestPos != null)
            {
                result.Add(bestPos);
                usedStrings.Add(bestPos.StringIndex);
                if (bestPos.Fret > 0)
                {
                    if (minFret == -1 || bestPos.Fret < minFret) minFret = bestPos.Fret;
                    if (maxFret == -1 || bestPos.Fret > maxFret) maxFret = bestPos.Fret;
                }
            }
            else
            {
                // Note cannot be played within constraints (e.g. out of range, or unplayable stretch).
                // In a robust engine, we'd octave shift or drop notes. For now, we omit it.
            }
        }

        return result;
    }
}
