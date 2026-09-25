using System.Threading;

namespace Chordality.App.Services.Midi;

public struct MidiEventRecord
{
    public long SampleTimestamp;
    public bool IsNoteOn;
    public byte Note;
    public byte Velocity;
}

public class MidiStashBuffer
{
    // Power of 2 capacity ensures safe integer overflow wrapping via bitwise AND
    // 65536 allows for roughly 20+ minutes of very dense continuous playing.
    private readonly MidiEventRecord[] _buffer;
    private readonly int _capacityMask;
    private readonly int _capacity;

    // Lock-free indices
    // _writeIndex is where we are writing next.
    // _commitIndex is where we have finished writing.
    private int _writeIndex = 0;
    private int _commitIndex = 0;

    public MidiStashBuffer()
    {
        _capacity = 65536; // 2^16
        _capacityMask = _capacity - 1; // 65535 (0xFFFF)
        _buffer = new MidiEventRecord[_capacity];
    }

    /// <summary>
    /// Appends a MIDI event to the circular stash buffer lock-free.
    /// Safe to call from the high-priority real-time audio thread.
    /// </summary>
    public void Append(long sampleTimestamp, bool isNoteOn, byte note, byte velocity)
    {
        // 1. Reserve slot atomically
        int index = Interlocked.Increment(ref _writeIndex) - 1;

        // Use bitwise mask to safely wrap around even when index hits int.MaxValue/MinValue
        int wrappedIndex = index & _capacityMask;

        // 2. Write data into reserved slot
        _buffer[wrappedIndex].SampleTimestamp = sampleTimestamp;
        _buffer[wrappedIndex].IsNoteOn = isNoteOn;
        _buffer[wrappedIndex].Note = note;
        _buffer[wrappedIndex].Velocity = velocity;

        // 3. Wait for our turn to commit (in case multiple threads interleave, though unlikely on single audio thread)
        // This makes it a true lock-free queue pattern for Multiple Producers.
        // (Since audio usually writes from 1 thread, this spin will almost always fall through instantly).
        var spinWait = new SpinWait();
        while (Interlocked.CompareExchange(ref _commitIndex, index + 1, index) != index)
        {
            spinWait.SpinOnce();
        }
    }

    /// <summary>
    /// Gets a snapshot of the current buffer. Safe to call from a background worker thread.
    /// </summary>
    public MidiEventRecord[] GetSnapshot(int count)
    {
        // Get the exact committed tail right now (guarantees data is written)
        int currentTail = Volatile.Read(ref _commitIndex);

        // If we haven't wrapped yet, clamp the count
        if (currentTail < count) count = currentTail;
        if (count > _capacity) count = _capacity;
        if (count <= 0) return System.Array.Empty<MidiEventRecord>();

        var snapshot = new MidiEventRecord[count];

        for (int i = 0; i < count; i++)
        {
            // Traverse backward from the newest event (currentTail - 1)
        int index = (currentTail - 1 - i) & _capacityMask;

            // Because we iterate backward from the tail,
            // array[0] is newest, array[count-1] is oldest.
            snapshot[i] = _buffer[index];
        }

        // Return chronological order (oldest first)
        System.Array.Reverse(snapshot);
        return snapshot;
    }
}
