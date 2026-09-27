# Chordality Architecture

Chordality was engineered under a strict 5-Phase Blueprint to ensure that the user interface, musical mathematics, and real-time digital signal processing (DSP) never block each other.

## 1. MVVM & Cross-Thread Safety

The project utilizes `CommunityToolkit.Mvvm`. To prevent UI Thread (WinUI 3) priority inversions from impacting the Real-time Audio Thread (NAudio WASAPI), they communicate exclusively via `ConcurrentQueue<Action>`.
When the UI triggers a chord change or BPM adjustment, the ViewModel drops an Action into the queue. The Audio thread safely dequeues and processes this right before reading the next audio buffer block.

## 2. Sample-Accurate Sequencing

Standard `Task.Delay` or `DispatcherTimer` classes rely on the Windows thread-pool and operate with 15-30ms of jitter. For professional audio (Arpeggiator, Strum delays), this is unacceptable.
`SequencingSampleProvider.cs` tracks exact fractional sample counts (`_currentSample`). It calculates events based on formula: `samplesToWait = (milliseconds / 1000.0) * WaveFormat.SampleRate`. It utilizes a split-rendering algorithm to execute MIDI events precisely at the required sample boundary mid-buffer.

## 3. Hardware MIDI Synchronization

To prevent the `Windows.Devices.Midi` COM interop from stuttering the NAudio render thread, the Audio sequencer raises `OnNoteOn` events, which are intercepted by `WindowsMidiService.cs`. The service places these in a `BlockingCollection`, where a dedicated, high-priority background thread dispatches them to the external hardware. This achieves dual-sync (Audio + MIDI) with microsecond precision and zero buffer dropouts.

## 4. The Lock-Free Stash Buffer

The "Stash" feature records 20 minutes of continuous playback. It is implemented in `MidiStashBuffer.cs` using a strictly lock-free circular array:
*   **Capacity:** 65,536 (Power of 2).
*   **No Allocations:** Structs are pre-allocated. `new` is forbidden on the audio thread to prevent Garbage Collection pauses.
*   **Atomic Wrapping:** `Interlocked.Increment` secures the index, and a bitwise mask (`index & 65535`) provides perfectly safe boundary wrapping, even when the C# `int` inevitably overflows into negative limits.