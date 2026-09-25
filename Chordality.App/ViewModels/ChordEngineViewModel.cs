using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Chordality.Engine;

namespace Chordality.App.ViewModels;

public partial class ChordEngineViewModel : ObservableObject
{
    [ObservableProperty]
    private PitchClass _rootPitch = PitchClass.C;

    [ObservableProperty]
    private int _octave = 4;

    [ObservableProperty]
    private BaseQuality _quality = BaseQuality.Major;

    [ObservableProperty]
    private int _inversion = 0;

    // Use ObservableCollection for modifiers so UI can bind to it
    public ObservableCollection<ChordModifier> Modifiers { get; } = new();

    private int[] _activeNotes = System.Array.Empty<int>();
    public int[] ActiveNotes
    {
        get => _activeNotes;
        private set => SetProperty(ref _activeNotes, value);
    }

    public ChordEngineViewModel()
    {
        Modifiers.CollectionChanged += (s, e) => UpdateNotes();
        UpdateNotes();
    }

    // Intercept property changes to trigger note regeneration
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName is nameof(RootPitch) or nameof(Octave) or nameof(Quality) or nameof(Inversion))
        {
            UpdateNotes();
        }
    }

    private void UpdateNotes()
    {
        ActiveNotes = Chord.GenerateNotes(RootPitch, Octave, Quality, Modifiers, Inversion);
    }

    public void ToggleModifier(ChordModifier modifier)
    {
        if (Modifiers.Contains(modifier))
        {
            Modifiers.Remove(modifier);
        }
        else
        {
            Modifiers.Add(modifier);
        }
    }
}
