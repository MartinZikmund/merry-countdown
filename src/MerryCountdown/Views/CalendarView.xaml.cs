using System.ComponentModel;
using MerryCountdown.Controls;
using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;

namespace MerryCountdown.Views;

[NavigationInfo(NavigationSection.Calendar)]
public partial class CalendarViewBase : ViewBase<CalendarViewModel> { }

public sealed partial class CalendarView : CalendarViewBase
{
    private SceneCanvas? _confetti;

    public CalendarView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (ViewModel is not null)
            {
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
        Unloaded += (_, _) =>
        {
            if (ViewModel is not null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }
        };
    }

    // The wall picks whichever arrangement of 24 gives the biggest doors on this screen.
    private void DoorScroller_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        const double Spacing = 8;
        const double SmallestDoor = 72;
        const double LargestDoor = 200;
        const double VerticalPadding = 24;

        var columns = 6;
        var door = 0d;
        foreach (var candidate in new[] { 3, 4, 6, 8, 12 })
        {
            var rows = 24 / candidate;
            var fitWidth = (e.NewSize.Width - ((candidate - 1) * Spacing)) / candidate;
            var fitHeight = (e.NewSize.Height - VerticalPadding - ((rows - 1) * Spacing)) / rows;
            if (Math.Min(fitWidth, fitHeight) > door)
            {
                door = Math.Min(fitWidth, fitHeight);
                columns = candidate;
            }
        }

        DoorLayout.MaximumRowsOrColumns = columns;
        DoorLayout.MinItemWidth = Math.Clamp(door, SmallestDoor, LargestDoor);
        DoorLayout.MinItemHeight = Math.Clamp(door, SmallestDoor, LargestDoor);
    }

    private void Door_Click(object sender, RoutedEventArgs e)
    {
        if (sender is DoorTile { Door: { } door })
        {
            ViewModel?.TapDoor(door);
        }
    }

    private void CloseReveal_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel?.IsRevealOpen == true)
        {
            ViewModel.CloseRevealCommand.Execute(null);
            args.Handled = true;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CalendarViewModel.Reveal))
        {
            return;
        }

        if (ViewModel?.Reveal is null)
        {
            RemoveConfetti();
        }
        else
        {
            CelebrateReveal();
        }
    }

    private void CelebrateReveal()
    {
        if (_confetti is null)
        {
            _confetti = SceneCanvas.CreateConfetti();
            RevealLayer.Children.Insert(0, _confetti);
        }

        Storyboard pop = new();
        foreach (var property in new[] { nameof(CompositeTransform.ScaleX), nameof(CompositeTransform.ScaleY) })
        {
            DoubleAnimation grow = new()
            {
                From = 0.6,
                To = 1,
                Duration = new Duration(TimeSpan.FromMilliseconds(320)),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 },
            };
            Storyboard.SetTarget(grow, RevealTransform);
            Storyboard.SetTargetProperty(grow, property);
            pop.Children.Add(grow);
        }

        pop.Begin();
        CloseRevealButton.Focus(FocusState.Programmatic);
    }

    private void RemoveConfetti()
    {
        if (_confetti is not null)
        {
            RevealLayer.Children.Remove(_confetti);
            _confetti = null;
        }
    }
}
