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
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        DrawCircleOfFifths();
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
