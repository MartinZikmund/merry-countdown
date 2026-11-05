using System.Globalization;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace MerryCountdown.Controls;

public sealed record DoorPaint(Brush Leaf, Brush Panel, Brush Wreath, Brush Number, Brush Frame, Brush Inside);

/// <summary>A drawn door: frame, leaf, wreath and what's behind it, with its swing and shake.</summary>
public sealed partial class DoorArt : UserControl
{
    private const double OpenedAngle = 78;
    private const double OpenedShade = 0.35;

    private readonly PlaneProjection _hinge = new() { CenterOfRotationX = 0 };

    public DoorArt() => InitializeComponent();

    public bool IsSwinging { get; private set; }

    /// <summary>On a wall of small doors, whether a door is locked or opened has to read at a glance.</summary>
    public void UseBigLockAndSticker()
    {
        Padlock.Width = Padlock.Height = 96;
        Padlock.Margin = new Thickness(0, 174, 0, 0);
        Padlock.HorizontalAlignment = HorizontalAlignment.Center;
        Sticker.Width = Sticker.Height = 140;
    }

    public void Show(int number, bool isLocked, bool isOpen, ImageSource? sticker, DoorPaint paint)
    {
        LeafFace.Background = paint.Leaf;
        UpperPanel.BorderBrush = LowerPanel.BorderBrush = paint.Panel;
        Wreath.Stroke = paint.Wreath;
        NumberText.Foreground = paint.Number;
        Frame.BorderBrush = paint.Frame;
        Inside.Background = paint.Inside;
        NumberText.Text = number.ToString(CultureInfo.CurrentCulture);
        Knob.Visibility = isLocked ? Visibility.Collapsed : Visibility.Visible;
        Padlock.Visibility = isLocked ? Visibility.Visible : Visibility.Collapsed;
        Sticker.Source = sticker;

        if (!IsSwinging)
        {
            SetSwing(isOpen);
        }
    }

    public void SwingOpen(Action completed)
    {
        if (IsSwinging)
        {
            return;
        }

        IsSwinging = true;
        Leaf.Projection = _hinge;

        var duration = new Duration(TimeSpan.FromMilliseconds(600));
        DoubleAnimation swing = new()
        {
            To = OpenedAngle,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },

            // Without this, Uno skips the rotation and the storyboard never completes.
            EnableDependentAnimation = true,
        };
        Storyboard.SetTarget(swing, _hinge);
        Storyboard.SetTargetProperty(swing, nameof(PlaneProjection.RotationY));

        DoubleAnimation shade = new()
        {
            To = OpenedShade,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(shade, LeafShade);
        Storyboard.SetTargetProperty(shade, nameof(Opacity));

        Storyboard storyboard = new();
        storyboard.Children.Add(swing);
        storyboard.Children.Add(shade);
        storyboard.Completed += (_, _) =>
        {
            // A finished storyboard keeps holding its values, which would pin the door open when the kid or day changes.
            storyboard.Stop();
            SetSwing(isOpen: true);
            IsSwinging = false;
            completed();
        };
        storyboard.Begin();
    }

    public void Shake()
    {
        DoubleAnimationUsingKeyFrames shake = new();
        foreach (var (ms, x) in new (double Ms, double X)[] { (0, 0), (70, -8), (140, 8), (210, -5), (280, 0) })
        {
            shake.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ms)), Value = x });
        }

        Storyboard.SetTarget(shake, Jiggle);
        Storyboard.SetTargetProperty(shake, nameof(Jiggle.X));
        Storyboard storyboard = new();
        storyboard.Children.Add(shake);
        storyboard.Begin();
    }

    // Shut doors skip the projection: it would soften their text and cost a layer for each of the 24.
    private void SetSwing(bool isOpen)
    {
        _hinge.RotationY = isOpen ? OpenedAngle : 0;
        LeafShade.Opacity = isOpen ? OpenedShade : 0;
        Leaf.Projection = isOpen ? _hinge : null;
    }
}
