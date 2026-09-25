using System;
using System.Collections.Generic;
using Xunit;
using Chordality.Engine;

namespace Chordality.Engine.Tests;

public class ChordTests
{
    [Fact]
    public void Pitch_GetMidiNote_ReturnsCorrectValue()
    {
        Assert.Equal(60, Pitch.GetMidiNote(PitchClass.C, 4));
        Assert.Equal(0, Pitch.GetMidiNote(PitchClass.C, -1));
        Assert.Equal(127, Pitch.GetMidiNote(PitchClass.G, 9));
        Assert.Equal(69, Pitch.GetMidiNote(PitchClass.A, 4));
    }

    [Fact]
    public void Pitch_ClampMidiNote_WorksCorrectly()
    {
        Assert.Equal(0, Pitch.ClampMidiNote(-5));
        Assert.Equal(60, Pitch.ClampMidiNote(60));
        Assert.Equal(127, Pitch.ClampMidiNote(135));
    }

    [Fact]
    public void Harmony_GetIntervals_MajorTriad()
    {
        var intervals = Harmony.GetIntervals(BaseQuality.Major, null);
        Assert.Equal(new[] { 0, 4, 7 }, intervals);
    }

    [Fact]
    public void Harmony_GetIntervals_MinorTriad()
    {
        var intervals = Harmony.GetIntervals(BaseQuality.Minor, null);
        Assert.Equal(new[] { 0, 3, 7 }, intervals);
    }

    [Fact]
    public void Harmony_GetIntervals_Sus4()
    {
        var intervals = Harmony.GetIntervals(BaseQuality.Major, new[] { ChordModifier.Sus4 });
        Assert.Equal(new[] { 0, 5, 7 }, intervals); // Third (4) is omitted
    }

    [Fact]
    public void Harmony_GetIntervals_Maj7()
    {
        var intervals = Harmony.GetIntervals(BaseQuality.Major, new[] { ChordModifier.Maj7 });
        Assert.Equal(new[] { 0, 4, 7, 11 }, intervals);
    }

    [Fact]
    public void Harmony_GetIntervals_Dom9()
    {
        // Root(0), Maj3(4), P5(7), Min7(10), Maj9(14)
        var intervals = Harmony.GetIntervals(BaseQuality.Major, new[] { ChordModifier.Dom7, ChordModifier.Nine });
        Assert.Equal(new[] { 0, 4, 7, 10, 14 }, intervals);
    }

    [Fact]
    public void Chord_GenerateNotes_CMajor()
    {
        var notes = Chord.GenerateNotes(PitchClass.C, 4, BaseQuality.Major, null);
        Assert.Equal(new[] { 60, 64, 67 }, notes);
    }

    [Fact]
    public void Chord_GenerateNotes_AMinor7()
    {
        // A3 = 57, C4 = 60, E4 = 64, G4 = 67
        var notes = Chord.GenerateNotes(PitchClass.A, 3, BaseQuality.Minor, new[] { ChordModifier.Dom7 });
        Assert.Equal(new[] { 57, 60, 64, 67 }, notes);
    }

    [Fact]
    public void Chord_GenerateNotes_Inversion1_Positive()
    {
        // C Major: 60, 64, 67
        // Inv 1: lowest (60) goes to 72 => 64, 67, 72
        var notes = Chord.GenerateNotes(PitchClass.C, 4, BaseQuality.Major, null, 1);
        Assert.Equal(new[] { 64, 67, 72 }, notes);
    }

    [Fact]
    public void Chord_GenerateNotes_Inversion2_Positive()
    {
        // C Major: 60, 64, 67
        // Inv 2: lowest two go up => 67, 72, 76
        var notes = Chord.GenerateNotes(PitchClass.C, 4, BaseQuality.Major, null, 2);
        Assert.Equal(new[] { 67, 72, 76 }, notes);
    }

    [Fact]
    public void Chord_GenerateNotes_Inversion1_Negative()
    {
        // C Major: 60, 64, 67
        // Inv -1: highest (67) goes down => 55, 60, 64
        var notes = Chord.GenerateNotes(PitchClass.C, 4, BaseQuality.Major, null, -1);
        Assert.Equal(new[] { 55, 60, 64 }, notes);
    }

    [Fact]
    public void Chord_GenerateNotes_Inversion3_Negative()
    {
        // C Major: 60, 64, 67
        // Inv -3: all 3 go down => 48, 52, 55
        var notes = Chord.GenerateNotes(PitchClass.C, 4, BaseQuality.Major, null, -3);
        Assert.Equal(new[] { 48, 52, 55 }, notes);
    }

    [Fact]
    public void Chord_GenerateNotes_Clamping_Top()
    {
        // G9 octave 9: G9 (127), B9 (131), D10 (134)
        // With clamping: 127, 127, 127 -> distinct -> 127
        var notes = Chord.GenerateNotes(PitchClass.G, 9, BaseQuality.Major, null);
        Assert.Equal(new[] { 127 }, notes);
    }

    [Fact]
    public void Chord_GenerateNotes_Clamping_Bottom()
    {
        // C-1: 0, 4, 7
        // Inv -1: highest (7) goes down => -5
        // Clamping: -5 -> 0 => 0, 0, 4 -> distinct -> 0, 4
        var notes = Chord.GenerateNotes(PitchClass.C, -1, BaseQuality.Major, null, -1);
        Assert.Equal(new[] { 0, 4 }, notes);
    }

    [Fact]
    public void Chord_GenerateNotes_ComplexChord()
    {
        // C4 (60), E (64), b5 (66), dom7 (70), b9 (73)
        var notes = Chord.GenerateNotes(PitchClass.C, 4, BaseQuality.Major,
            new[] { ChordModifier.Flat5, ChordModifier.Dom7, ChordModifier.Flat9 });
        Assert.Equal(new[] { 60, 64, 66, 70, 73 }, notes);
    }
}
