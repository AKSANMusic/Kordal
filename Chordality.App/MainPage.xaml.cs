using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Chordality.App.ViewModels;
using Chordality.Engine;

namespace Chordality_App;

public sealed partial class MainPage : Page
{
    public ChordEngineViewModel ViewModel { get; } = new();

    private const double CircleRadius = 150;
    private const double MinorCircleRadius = 100;

    // Circle of Fifths layout mapping (0 = C)
    private readonly PitchClass[] _circleOfFifthsMajor = new[]
    {
        PitchClass.C, PitchClass.G, PitchClass.D, PitchClass.A, PitchClass.E, PitchClass.B,
        PitchClass.FSharp, PitchClass.CSharp, PitchClass.GSharp, PitchClass.DSharp, PitchClass.ASharp, PitchClass.F
    };

    // Relative minors mapped to the exact same angles as their major counterparts
    private readonly PitchClass[] _circleOfFifthsMinor = new[]
    {
        PitchClass.A, PitchClass.E, PitchClass.B, PitchClass.FSharp, PitchClass.CSharp, PitchClass.GSharp,
        PitchClass.DSharp, PitchClass.ASharp, PitchClass.F, PitchClass.C, PitchClass.G, PitchClass.D
    };

    public MainPage()
    {
        InitializeComponent();
        Loaded += MainPage_Loaded;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        DrawCircleOfFifths();
        DrawPiano();
        DrawGuitarFretboard();
        UpdateVisualizers();
    }

    private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ChordEngineViewModel.ActiveNotesDisplay))
        {
            UpdateVisualizers();
        }
    }

    private void SetPadMode(object sender, RoutedEventArgs e) { SwitchVisualizerMode(ChordEngineViewModel.VisualizerMode.Pad); }
    private void SetNoteBlockMode(object sender, RoutedEventArgs e) { SwitchVisualizerMode(ChordEngineViewModel.VisualizerMode.NoteBlock); }
    private void SetPianoMode(object sender, RoutedEventArgs e) { SwitchVisualizerMode(ChordEngineViewModel.VisualizerMode.Piano); }
    private void SetGuitarMode(object sender, RoutedEventArgs e) { SwitchVisualizerMode(ChordEngineViewModel.VisualizerMode.Guitar); }

    private void SwitchVisualizerMode(ChordEngineViewModel.VisualizerMode mode)
    {
        ViewModel.CurrentMode = mode;
        PadViewContainer.Visibility = mode == ChordEngineViewModel.VisualizerMode.Pad ? Visibility.Visible : Visibility.Collapsed;
        NoteBlockViewContainer.Visibility = mode == ChordEngineViewModel.VisualizerMode.NoteBlock ? Visibility.Visible : Visibility.Collapsed;
        PianoViewContainer.Visibility = mode == ChordEngineViewModel.VisualizerMode.Piano ? Visibility.Visible : Visibility.Collapsed;
        GuitarViewContainer.Visibility = mode == ChordEngineViewModel.VisualizerMode.Guitar ? Visibility.Visible : Visibility.Collapsed;
        UpdateVisualizers();
    }

    private void UpdateVisualizers()
    {
        if (ViewModel.CurrentMode == ChordEngineViewModel.VisualizerMode.Piano)
        {
            DrawPiano();
        }
        else if (ViewModel.CurrentMode == ChordEngineViewModel.VisualizerMode.Guitar)
        {
            DrawGuitarFretboard();
        }
    }

    // -- Piano View Logic --

    private void DrawPiano()
    {
        PianoCanvas.Children.Clear();
        // Standard range: C3 (48) to B5 (83) -> 3 octaves
        int startNote = 48;
        int endNote = 83;

        double whiteKeyWidth = 25;
        double whiteKeyHeight = 150;
        double blackKeyWidth = 15;
        double blackKeyHeight = 90;

        int whiteKeyCount = 0;

        // Arrays to map indices to key colors
        bool[] isBlack = { false, true, false, true, false, false, true, false, true, false, true, false };

        // 1st Pass: Draw White Keys
        for (int note = startNote; note <= endNote; note++)
        {
            int pitchClass = PitchFormatter.GetPitchClass(note);
            if (!isBlack[pitchClass])
            {
                bool isActive = Array.Exists(ViewModel.ActiveNotes, n => n == note);

                var rect = new Rectangle
                {
                    Width = whiteKeyWidth,
                    Height = whiteKeyHeight,
                    Fill = new SolidColorBrush(isActive ? ColorHelper.FromArgb(255, 255, 128, 0) : Colors.White), // Neon Orange or White
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(rect, whiteKeyCount * whiteKeyWidth);
                Canvas.SetTop(rect, 0);
                PianoCanvas.Children.Add(rect);
                whiteKeyCount++;
            }
        }

        // 2nd Pass: Draw Black Keys (drawn on top)
        whiteKeyCount = 0;
        for (int note = startNote; note <= endNote; note++)
        {
            int pitchClass = PitchFormatter.GetPitchClass(note);
            if (isBlack[pitchClass])
            {
                bool isActive = Array.Exists(ViewModel.ActiveNotes, n => n == note);

                var rect = new Rectangle
                {
                    Width = blackKeyWidth,
                    Height = blackKeyHeight,
                    Fill = new SolidColorBrush(isActive ? ColorHelper.FromArgb(255, 255, 128, 0) : Colors.Black), // Neon Orange or Black
                    Stroke = new SolidColorBrush(Colors.Black),
                    StrokeThickness = 1
                };
                Canvas.SetLeft(rect, (whiteKeyCount * whiteKeyWidth) - (blackKeyWidth / 2));
                Canvas.SetTop(rect, 0);
                PianoCanvas.Children.Add(rect);
            }
            else
            {
                whiteKeyCount++;
            }
        }
    }

    // -- Guitar View Logic --

    private void DrawGuitarFretboard()
    {
        GuitarCanvas.Children.Clear();

        double stringSpacing = 25;
        double fretSpacing = 50;

        int numStrings = 6;
        int numFrets = GuitarVisualizer.MaxFrets;

        // Draw Strings (Horizontal lines)
        for (int i = 0; i < numStrings; i++)
        {
            var line = new Line
            {
                X1 = 0,
                Y1 = i * stringSpacing + 20,
                X2 = numFrets * fretSpacing,
                Y2 = i * stringSpacing + 20,
                Stroke = new SolidColorBrush(Colors.Gray),
                StrokeThickness = 2
            };
            GuitarCanvas.Children.Add(line);
        }

        // Draw Frets (Vertical lines)
        for (int i = 0; i <= numFrets; i++)
        {
            var line = new Line
            {
                X1 = i * fretSpacing,
                Y1 = 20,
                X2 = i * fretSpacing,
                Y2 = (numStrings - 1) * stringSpacing + 20,
                Stroke = new SolidColorBrush(i == 0 ? Colors.White : Colors.DarkGray),
                StrokeThickness = i == 0 ? 4 : 2
            };
            GuitarCanvas.Children.Add(line);
        }

        // Calculate and Draw Fingerings
        var fingerings = GuitarVisualizer.CalculateFingering(ViewModel.ActiveNotes);
        foreach (var pos in fingerings)
        {
            // Note: Strings are visually drawn from top (E4) to bottom (E2).
            // StandardTuning array is from E2 to E4 (index 0 to 5).
            // So visual string index is (5 - pos.StringIndex).
            int visualStringIndex = 5 - pos.StringIndex;

            double x = pos.Fret == 0 ? -10 : (pos.Fret * fretSpacing) - (fretSpacing / 2);
            double y = visualStringIndex * stringSpacing + 20;

            var ellipse = new Ellipse
            {
                Width = 20,
                Height = 20,
                Fill = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 255, 0)) // Neon Green
            };

            Canvas.SetLeft(ellipse, x - 10);
            Canvas.SetTop(ellipse, y - 10);
            GuitarCanvas.Children.Add(ellipse);

            var text = new TextBlock
            {
                Text = PitchFormatter.GetNoteName(pos.MidiNote),
                Foreground = new SolidColorBrush(Colors.Black),
                FontSize = 10,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Canvas.SetLeft(text, x - 8);
            Canvas.SetTop(text, y - 8);
            GuitarCanvas.Children.Add(text);
        }
    }

    private void DrawCircleOfFifths()
    {
        CircleCanvas.Children.Clear();

        // Draw center reference crosshair
        var cx = CircleCanvas.Width / 2;
        var cy = CircleCanvas.Height / 2;

        // Draw nodes
        for (int i = 0; i < 12; i++)
        {
            // Calculate angle. 0 is at 12 o'clock, progressing clockwise.
            // Math.Sin/Cos use radians, starting from 3 o'clock (0 rad).
            // So 12 o'clock is -PI/2.
            double angle = (i * 30 - 90) * (Math.PI / 180);

            // Major Node
            DrawNode(cx + CircleRadius * Math.Cos(angle), cy + CircleRadius * Math.Sin(angle), _circleOfFifthsMajor[i].ToString().Replace("Sharp", "#"), true);

            // Minor Node
            DrawNode(cx + MinorCircleRadius * Math.Cos(angle), cy + MinorCircleRadius * Math.Sin(angle), _circleOfFifthsMinor[i].ToString().Replace("Sharp", "#") + "m", false);
        }
    }

    private void DrawNode(double x, double y, string label, bool isMajor)
    {
        var ellipse = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = new SolidColorBrush(isMajor ? Colors.White : Colors.Gray)
        };
        Canvas.SetLeft(ellipse, x - 5);
        Canvas.SetTop(ellipse, y - 5);
        CircleCanvas.Children.Add(ellipse);

        var text = new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(isMajor ? Colors.White : Colors.Gray),
            FontSize = 12,
            FontFamily = new FontFamily("Consolas")
        };

        // Minor tweaks for label positioning
        Canvas.SetLeft(text, x - 10);
        Canvas.SetTop(text, y + 10);
        CircleCanvas.Children.Add(text);
    }

    private void CircleCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        HandlePointer(e, false);
    }

    private void CircleCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        HandlePointer(e, true);
    }

    private void HandlePointer(PointerRoutedEventArgs e, bool force)
    {
        var ptr = e.GetCurrentPoint(CircleCanvas);
        if (!ptr.Properties.IsLeftButtonPressed && !force) return;

        double cx = CircleCanvas.Width / 2;
        double cy = CircleCanvas.Height / 2;

        double x = ptr.Position.X - cx;
        double y = ptr.Position.Y - cy;

        double distance = Math.Sqrt(x * x + y * y);
        if (distance < 50 || distance > 200) return; // Ignore center typography or way outside

        // Calculate angle in degrees from 12 o'clock
        double angleRad = Math.Atan2(y, x);
        double angleDeg = (angleRad * 180 / Math.PI) + 90; // Shift 0 to 12 o'clock
        if (angleDeg < 0) angleDeg += 360;

        // Snap to nearest 30 degree segment (0 to 11)
        int segment = (int)Math.Round(angleDeg / 30.0) % 12;

        if (distance > (MinorCircleRadius + (CircleRadius - MinorCircleRadius) / 2))
        {
            // Outer ring (Major)
            ViewModel.RootPitch = _circleOfFifthsMajor[segment];
            ViewModel.Quality = BaseQuality.Major;
        }
        else
        {
            // Inner ring (Minor)
            ViewModel.RootPitch = _circleOfFifthsMinor[segment];
            ViewModel.Quality = BaseQuality.Minor;
        }
    }
}
