using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Chordality.Engine;
using Chordality.Audio;
using Chordality.App.Services.Midi;

namespace Chordality.App.ViewModels;

public partial class ModifierItem : ObservableObject
{
    public ChordModifier Modifier { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isActive;

    public ModifierItem(ChordModifier modifier, string displayName)
    {
        Modifier = modifier;
        DisplayName = displayName;
    }
}

public partial class ChordEngineViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveChordName))]
    private PitchClass _rootPitch = PitchClass.C;

    [ObservableProperty]
    private int _octave = 4;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ActiveChordName))]
    private BaseQuality _quality = BaseQuality.Major;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(InversionDisplay))]
    private int _inversion = 0;

    public string InversionDisplay => $"Inv {_inversion}";

    public ObservableCollection<ChordModifier> Modifiers { get; } = new();

    public ObservableCollection<ModifierItem> ModifierItems { get; } = new();

    public enum VisualizerMode
    {
        Pad,
        NoteBlock,
        Piano,
        Guitar
    }

    [ObservableProperty]
    private VisualizerMode _currentMode = VisualizerMode.Pad;

    [ObservableProperty]
    private PlaybackMode _audioMode = PlaybackMode.Chord;

    [ObservableProperty]
    private double _bpm = 120.0;

    private int[] _activeNotes = Array.Empty<int>();
    public int[] ActiveNotes
    {
        get => _activeNotes;
        private set
        {
            if (SetProperty(ref _activeNotes, value))
            {
                OnPropertyChanged(nameof(ActiveNotesDisplay));
                OnPropertyChanged(nameof(NoteBlocks));
            }
        }
    }

    public string ActiveChordName => $"{RootPitch.ToString().Replace("Sharp", "#")} {(Quality == BaseQuality.Minor ? "m" : "")}";

    public string ActiveNotesDisplay => string.Join("  ", ActiveNotes);

    public ObservableCollection<string> NoteBlocks => new(ActiveNotes.Select(PitchFormatter.GetNoteName));

    // Audio & MIDI instances
    private AudioPlaybackEngine? _audioEngine;
    public WindowsMidiService MidiService { get; } = new();
    private readonly MidiStashBuffer _stashBuffer = new();

    [ObservableProperty]
    private MidiDeviceInfo? _selectedMidiDevice;

    [ObservableProperty]
    private bool _isMidiEnabled = false;

    public ChordEngineViewModel()
    {
        InitializeModifiers();

        try
        {
            _audioEngine = new AudioPlaybackEngine();
            // Route NAudio Sequencer events to external MIDI queue and internal Stash Buffer
            _audioEngine.Synthesizer.OnNoteOnFired += (note, velocity) =>
            {
                // Background thread safe
                long timestamp = _audioEngine.Sequencer.CurrentSample;
                _stashBuffer.Append(timestamp, true, (byte)note, (byte)(velocity * 127));

                if (IsMidiEnabled && MidiService.IsConnected)
                    MidiService.SendNoteOn(0, (byte)note, (byte)(velocity * 127));
            };

            _audioEngine.Synthesizer.OnNoteOffFired += (note) =>
            {
                // Background thread safe
                long timestamp = _audioEngine.Sequencer.CurrentSample;
                _stashBuffer.Append(timestamp, false, (byte)note, 0);

                if (IsMidiEnabled && MidiService.IsConnected)
                    MidiService.SendNoteOff(0, (byte)note);
            };
        }
        catch
        {
            // Failsafe: if audio totally fails to init, we proceed silently for UI testing.
        }

        Modifiers.CollectionChanged += (s, e) => UpdateNotes();
        UpdateNotes();
    }

    private void InitializeModifiers()
    {
        var modifiers = new[]
        {
            (ChordModifier.Maj7, "Maj7"),
            (ChordModifier.Dom7, "7"),
            (ChordModifier.Six, "6"),
            (ChordModifier.Sus2, "Sus2"),
            (ChordModifier.Sus4, "Sus4"),
            (ChordModifier.Nine, "9"),
            (ChordModifier.Eleven, "11"),
            (ChordModifier.Thirteen, "13"),
            (ChordModifier.Flat5, "b5"),
            (ChordModifier.Sharp5, "#5"),
            (ChordModifier.Flat9, "b9"),
            (ChordModifier.Sharp9, "#9"),
            (ChordModifier.Sharp11, "#11"),
            (ChordModifier.Flat13, "b13"),
            (ChordModifier.Dim, "Dim"),
            (ChordModifier.Aug, "Aug")
        };

        foreach (var (mod, name) in modifiers)
        {
            var item = new ModifierItem(mod, name);
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ModifierItem.IsActive))
                {
                    if (item.IsActive && !Modifiers.Contains(item.Modifier))
                        Modifiers.Add(item.Modifier);
                    else if (!item.IsActive && Modifiers.Contains(item.Modifier))
                        Modifiers.Remove(item.Modifier);
                }
            };
            ModifierItems.Add(item);
        }
    }

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(RootPitch) or nameof(Octave) or nameof(Quality) or nameof(Inversion))
        {
            UpdateNotes();
        }
        else if (e.PropertyName == nameof(AudioMode))
        {
            _audioEngine?.Sequencer.SetMode(AudioMode);
            // Re-trigger notes in the new mode
            _audioEngine?.Sequencer.UpdateNotes(ActiveNotes);
        }
        else if (e.PropertyName == nameof(Bpm))
        {
            _audioEngine?.Sequencer.SetBpm(Bpm);
        }
        else if (e.PropertyName == nameof(SelectedMidiDevice))
        {
            if (SelectedMidiDevice != null)
            {
                _ = MidiService.ConnectAsync(SelectedMidiDevice.Id);
            }
            else
            {
                MidiService.Disconnect();
            }
        }
        else if (e.PropertyName == nameof(IsMidiEnabled))
        {
            if (!IsMidiEnabled)
            {
                MidiService.SendAllNotesOff(0);
            }
        }
    }

    private void UpdateNotes()
    {
        ActiveNotes = Chord.GenerateNotes(RootPitch, Octave, Quality, Modifiers, Inversion);
        _audioEngine?.Sequencer.UpdateNotes(ActiveNotes);
    }

    public void CleanupAudio()
    {
        _audioEngine?.Dispose();
        _audioEngine = null;
        MidiService.Dispose();
    }

    [RelayCommand]
    private void InversionUp()
    {
        Inversion++;
    }

    [RelayCommand]
    private void InversionDown()
    {
        Inversion--;
    }

    [RelayCommand]
    private void InversionRoot()
    {
        Inversion = 0;
    }

    [RelayCommand]
    private async System.Threading.Tasks.Task ExportStashAsync(string durationParam)
    {
        if (_audioEngine == null) return;

        // durationParam represents minutes
        if (!int.TryParse(durationParam, out int minutes)) return;

        // Estimate number of events max we need (rough bound based on max dense playing)
        // 50 events per sec * 60 = 3000 events/min
        int countToSnapshot = minutes * 3000;

        var snapshot = _stashBuffer.GetSnapshot(countToSnapshot);

        // Let exporter run on background task
        await System.Threading.Tasks.Task.Run(async () =>
        {
            string path = await MidiExporter.ExportStashAsync(snapshot, Bpm, _audioEngine.Sequencer.WaveFormat.SampleRate);

            if (!string.IsNullOrEmpty(path))
            {
                System.Diagnostics.Debug.WriteLine($"Exported MIDI Stash to: {path}");

                // Trigger a File Explorer pop-up to show the saved file to the user
                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{path}\"");
            }
        });
    }
}
