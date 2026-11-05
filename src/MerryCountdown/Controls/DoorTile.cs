using System.ComponentModel;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;
using Windows.UI.ViewManagement;

namespace MerryCountdown.Controls;

/// <summary>One door on the wall. Locked doors look faded and padlocked; opened ones stay ajar with their sticker showing.</summary>
public sealed partial class DoorTile : Button
{
    public static readonly DependencyProperty DoorProperty = DependencyProperty.Register(
        nameof(Door), typeof(DoorViewModel), typeof(DoorTile), new PropertyMetadata(null, (d, e) => ((DoorTile)d).OnDoorChanged((DoorViewModel?)e.OldValue, (DoorViewModel?)e.NewValue)));

    private static readonly Color _lockedTone = Color.FromArgb(0xFF, 0x5D, 0x4A, 0x55);

    private readonly DoorArt _art = new();
    private readonly CompositeTransform _transform = new();
    private readonly AccessibilitySettings _accessibility = new();
    private Storyboard? _todayPulse;
    private ImageSource? _sticker;
    private string? _stickerKey;

    public DoorTile()
    {
        Style = (Style)Application.Current.Resources["DoorButtonStyle"];
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        RenderTransform = _transform;

        _art.UseBigLockAndSticker();
        Content = _art;

        Click += OnClick;
        Unloaded += (_, _) => StopPulse();
        ActualThemeChanged += (_, _) =>
        {
            if (Door is { } door)
            {
                Render(door);
            }
        };
    }

    /// <summary>Raised once a tapped door has finished swinging, or straight away if it doesn't swing.</summary>
    public event EventHandler<DoorViewModel>? DoorChosen;

    public DoorViewModel? Door
    {
        get => (DoorViewModel?)GetValue(DoorProperty);
        set => SetValue(DoorProperty, value);
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        if (Door is not { } door || _art.IsSwinging)
        {
            return;
        }

        if (door.ShouldSwingOpen)
        {
            StopPulse();
            Canvas.SetZIndex(this, 1);
            _art.SwingOpen(() => DoorChosen?.Invoke(this, door));
        }
        else
        {
            DoorChosen?.Invoke(this, door);
        }
    }

    private void OnDoorChanged(DoorViewModel? old, DoorViewModel? door)
    {
        if (old is not null)
        {
            old.PropertyChanged -= OnDoorPropertyChanged;
            old.Jiggled -= OnJiggled;
        }

        if (door is not null)
        {
            door.PropertyChanged += OnDoorPropertyChanged;
            door.Jiggled += OnJiggled;
            Render(door);
        }
    }

    private void OnDoorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is DoorViewModel door)
        {
            Render(door);
        }
    }

    private void Render(DoorViewModel door)
    {
        AutomationProperties.SetName(this, door.AccessibleName);
        AutomationProperties.SetAutomationId(this, $"Door{door.Number}");

        var isLocked = door.State == DoorState.Locked;
        var isOpened = door.State == DoorState.Opened;
        var showFrameAsToday = door.IsToday && !isOpened;

        DoorPaint paint = new(
            Leaf: Tone(Brush($"DoorTint{door.Tint}Brush"), isLocked),
            Panel: Brush("DoorBorderBrush"),
            Wreath: Tone(Brush("DoorWreathBrush"), isLocked),
            Number: Brush("DoorNumberBrush"),
            Frame: Brush(showFrameAsToday ? "DoorTodayBorderBrush" : "DoorFrameBrush"),
            Inside: Brush("DoorOpenedBrush"));

        // A locked door has nothing behind it yet, so its surprise can't peek out.
        _art.Show(door.Number, isLocked, isOpened, isLocked ? null : StickerFor(door), paint);

        // An open door sticks out of its cell, so it goes over its neighbours.
        Canvas.SetZIndex(this, isOpened || _art.IsSwinging ? 1 : 0);

        if (door.IsToday && door.State == DoorState.Ready && !door.CalmMotion && !_art.IsSwinging)
        {
            StartPulse();
        }
        else
        {
            StopPulse();
        }
    }

    private ImageSource StickerFor(DoorViewModel door)
    {
        if (_sticker is null || _stickerKey != door.Sticker)
        {
            _stickerKey = door.Sticker;
            _sticker = Stickers.Image(door.Sticker);
        }

        return _sticker;
    }

    // Locked doors keep their colour variety, just faded towards one quiet tone. High contrast has its own system colours.
    private Brush Tone(Brush brush, bool isLocked)
    {
        if (!isLocked || _accessibility.HighContrast || brush is not SolidColorBrush { Color: var color })
        {
            return brush;
        }

        return new SolidColorBrush(ColorHelper.FromArgb(0xFF, Halfway(color.R, _lockedTone.R), Halfway(color.G, _lockedTone.G), Halfway(color.B, _lockedTone.B)));

        static byte Halfway(byte a, byte b) => (byte)((a + b) / 2);
    }

    // ThemeResource lookups from code follow the app theme, but the user's choice sits on the root element.
    private Brush Brush(string key)
    {
        var theme = _accessibility.HighContrast ? "HighContrast" : ActualTheme == ElementTheme.Light ? "Light" : "Dark";
        foreach (var dictionary in Application.Current.Resources.MergedDictionaries)
        {
            if (dictionary.ThemeDictionaries.TryGetValue(theme, out var themed)
                && themed is ResourceDictionary resources
                && resources.TryGetValue(key, out var value))
            {
                return (Brush)value;
            }
        }

        return (Brush)Application.Current.Resources[key];
    }

    private void OnJiggled(object? sender, EventArgs e)
    {
        if (Door?.CalmMotion == false)
        {
            _art.Shake();
        }
    }

    private void StartPulse()
    {
        if (_todayPulse is not null)
        {
            return;
        }

        Storyboard storyboard = new() { RepeatBehavior = RepeatBehavior.Forever };
        foreach (var property in new[] { nameof(CompositeTransform.ScaleX), nameof(CompositeTransform.ScaleY) })
        {
            DoubleAnimationUsingKeyFrames pulse = new();
            pulse.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero), Value = 1 });
            pulse.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(700)), Value = 1.05 });
            pulse.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1400)), Value = 1 });
            Storyboard.SetTarget(pulse, _transform);
            Storyboard.SetTargetProperty(pulse, property);
            storyboard.Children.Add(pulse);
        }

        storyboard.Begin();
        _todayPulse = storyboard;
    }

    private void StopPulse()
    {
        _todayPulse?.Stop();
        _todayPulse = null;
        _transform.ScaleX = 1;
        _transform.ScaleY = 1;
    }
}
