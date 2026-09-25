using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Chordality.Engine;

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

    private int[] _activeNotes = Array.Empty<int>();
    public int[] ActiveNotes
    {
        get => _activeNotes;
        private set
        {
            if (SetProperty(ref _activeNotes, value))
            {
                OnPropertyChanged(nameof(ActiveNotesDisplay));
            }
        }
    }

    public string ActiveChordName => $"{RootPitch.ToString().Replace("Sharp", "#")} {(Quality == BaseQuality.Minor ? "m" : "")}";

    public string ActiveNotesDisplay => string.Join("  ", ActiveNotes);

    public ChordEngineViewModel()
    {
        InitializeModifiers();
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
    }

    private void UpdateNotes()
    {
        ActiveNotes = Chord.GenerateNotes(RootPitch, Octave, Quality, Modifiers, Inversion);
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
}
