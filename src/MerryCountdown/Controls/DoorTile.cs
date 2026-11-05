using System.ComponentModel;
using System.Globalization;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI.ViewManagement;

namespace MerryCountdown.Controls;

/// <summary>One little door. Built in code because it animates a lot and is repeated 24 times.</summary>
public sealed partial class DoorTile : Button
{
    public static readonly DependencyProperty DoorProperty = DependencyProperty.Register(
        nameof(Door), typeof(DoorViewModel), typeof(DoorTile), new PropertyMetadata(null, (d, e) => ((DoorTile)d).OnDoorChanged((DoorViewModel?)e.OldValue, (DoorViewModel?)e.NewValue)));

    private static readonly TimeSpan _flipHalf = TimeSpan.FromMilliseconds(160);

    private readonly Border _face = new() { CornerRadius = new CornerRadius(12), BorderThickness = new Thickness(3) };
    private readonly TextBlock _number = new() { HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    private readonly Image _sticker = new() { Margin = new Thickness(12), Stretch = Stretch.Uniform };
    private readonly CompositeTransform _transform = new();
    private readonly AccessibilitySettings _accessibility = new();
    private Storyboard? _todayPulse;
    private DoorState? _shownState;

    public DoorTile()
    {
        Padding = new Thickness(0);
        BorderThickness = new Thickness(0);
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5);
        RenderTransform = _transform;

        _number.Style = (Style)Application.Current.Resources["DoorNumberTextStyle"];

        Grid content = new();
        content.Children.Add(_number);
        content.Children.Add(_sticker);
        _face.Child = content;
        Content = _face;

        Unloaded += (_, _) => StopPulse();
        ActualThemeChanged += (_, _) =>
        {
            if (Door is { } door)
            {
                Render(door);
            }
        };
    }

    public DoorViewModel? Door
    {
        get => (DoorViewModel?)GetValue(DoorProperty);
        set => SetValue(DoorProperty, value);
    }

    private void OnDoorChanged(DoorViewModel? old, DoorViewModel? door)
    {
        if (old is not null)
        {
            old.PropertyChanged -= OnDoorPropertyChanged;
            old.Jiggled -= OnJiggled;
        }

        _shownState = null;
        if (door is not null)
        {
            door.PropertyChanged += OnDoorPropertyChanged;
            door.Jiggled += OnJiggled;
            _number.Text = door.Number.ToString(CultureInfo.CurrentCulture);
            Render(door);
        }
    }

    private void OnDoorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not DoorViewModel door)
        {
            return;
        }

        if (e.PropertyName == nameof(DoorViewModel.State) && _shownState is { } previous && previous != DoorState.Opened && door.State == DoorState.Opened)
        {
            Flip(door);
        }
        else
        {
            Render(door);
        }
    }

    private void Render(DoorViewModel door)
    {
        _shownState = door.State;
        AutomationProperties.SetName(this, door.AccessibleName);
        AutomationProperties.SetAutomationId(this, $"Door{door.Number}");

        var isOpened = door.State == DoorState.Opened;
        _sticker.Visibility = isOpened ? Visibility.Visible : Visibility.Collapsed;
        _number.Visibility = isOpened ? Visibility.Collapsed : Visibility.Visible;
        _sticker.Source = isOpened ? Stickers.Image(door.Sticker) : null;

        _face.Background = isOpened ? Brush("DoorOpenedBrush") : Brush($"DoorTint{door.Tint}Brush");
        _face.BorderBrush = door.IsToday && !isOpened ? Brush("DoorTodayBorderBrush") : Brush("DoorBorderBrush");
        _number.Foreground = Brush("DoorNumberBrush");
        _face.Opacity = door.State == DoorState.Locked ? 0.82 : 1;

        if (door.IsToday && door.State == DoorState.Ready)
        {
            StartPulse();
        }
        else
        {
            StopPulse();
        }
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

    private void Flip(DoorViewModel door)
    {
        StopPulse();

        var shut = Animate(nameof(CompositeTransform.ScaleX), 1, 0, _flipHalf, EasingMode.EaseIn);
        shut.Completed += (_, _) =>
        {
            Render(door);
            Animate(nameof(CompositeTransform.ScaleX), 0, 1, _flipHalf, EasingMode.EaseOut).Begin();
        };
        shut.Begin();
    }

    private void OnJiggled(object? sender, EventArgs e)
    {
        Storyboard storyboard = new();
        DoubleAnimationUsingKeyFrames shake = new();
        double[] angles = [0, -7, 6, -4, 3, 0];
        for (var i = 0; i < angles.Length; i++)
        {
            shake.KeyFrames.Add(new LinearDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(i * 70)),
                Value = angles[i],
            });
        }

        Storyboard.SetTarget(shake, _transform);
        Storyboard.SetTargetProperty(shake, nameof(CompositeTransform.Rotation));
        storyboard.Children.Add(shake);
        storyboard.Begin();
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

    private Storyboard Animate(string property, double from, double to, TimeSpan duration, EasingMode mode)
    {
        DoubleAnimation animation = new()
        {
            From = from,
            To = to,
            Duration = new Duration(duration),
            EasingFunction = new QuadraticEase { EasingMode = mode },
        };
        Storyboard.SetTarget(animation, _transform);
        Storyboard.SetTargetProperty(animation, property);
        Storyboard storyboard = new();
        storyboard.Children.Add(animation);
        return storyboard;
    }
}
