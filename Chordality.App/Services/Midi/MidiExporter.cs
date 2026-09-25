using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Midi;
using Windows.Storage;

namespace Chordality.App.Services.Midi;

public static class MidiExporter
{
    /// <summary>
    /// Serializes the lock-free snapshot into a Standard MIDI File (.mid).
    /// Safe to call on a background task.
    /// </summary>
    public static async Task<string> ExportStashAsync(MidiEventRecord[] records, double currentBpm, int sampleRate)
    {
        if (records == null || records.Length == 0) return string.Empty;

        var collection = new MidiEventCollection(0, 120); // 0 = Format 0 (single track), 120 ticks per quarter note
        int ticksPerQuarterNote = 120;

        // Add tempo meta event (micro-seconds per quarter note)
        int microSecondsPerQuarterNote = (int)(60000000.0 / currentBpm);
        collection.AddEvent(new TempoEvent(microSecondsPerQuarterNote, 0), 0);

        // Calculate timing conversion:
        // A quarter note = ticksPerQuarterNote (120)
        // Beats per second = BPM / 60
        // Quarter notes per sample = (BPM / 60) / SampleRate
        // Ticks per sample = QuarterNotesPerSample * 120
        double ticksPerSample = ((currentBpm / 60.0) / sampleRate) * ticksPerQuarterNote;

        // Base timestamp on the first event in the snapshot
        long startSample = records[0].SampleTimestamp;

        foreach (var rec in records)
        {
            // Convert exact NAudio sample offset into standard MIDI ticks
            long sampleDelta = rec.SampleTimestamp - startSample;
            long absoluteTick = (long)(sampleDelta * ticksPerSample);

            MidiEvent midiEvent;
            if (rec.IsNoteOn)
            {
                // NAudio requires duration for NoteOnEvent, but since we have discrete NoteOff events,
                // we technically just insert NoteEvent for NoteOn/NoteOff manually, or use NoteOnEvent with duration 0
                // We'll use NoteEvent directly as NoteOn/NoteOff commands
                midiEvent = new NoteEvent(absoluteTick, 1, MidiCommandCode.NoteOn, rec.Note, rec.Velocity);
            }
            else
            {
                midiEvent = new NoteEvent(absoluteTick, 1, MidiCommandCode.NoteOff, rec.Note, 0);
            }

            collection.AddEvent(midiEvent, 0);
        }

        // Must cap the track with an End Track event
        long lastTick = (long)((records[^1].SampleTimestamp - startSample) * ticksPerSample);
        collection.AddEvent(new MetaEvent(MetaEventType.EndTrack, 0, lastTick), 0);

        collection.PrepareForExport();

        // Write to local temp storage
        string fileName = $"Chordality_Stash_{DateTime.Now:yyyyMMdd_HHmmss}.mid";
        var localFolder = ApplicationData.Current.LocalFolder;
        var file = await localFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

        MidiFile.Export(file.Path, collection);

        return file.Path;
    }
}
