using System.ComponentModel;
using MerryCountdown.Controls;
using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.Season;
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

            // The kid picker outlives this page, and its x:Bind subscription would keep the view model alive with it.
            Bindings.StopTracking();
        };
    }

    private void DoorScroller_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        const double WallMarginHeight = 48;
        const double WallMarginWidth = 16;
        const double ScrollBarGutter = 20;

        var layout = DoorWallLayout.Fit(e.NewSize.Width - WallMarginWidth - ScrollBarGutter, e.NewSize.Height - WallMarginHeight);
        DoorLayout.MaximumRowsOrColumns = layout.Columns;
        DoorLayout.MinItemWidth = layout.DoorWidth;
        DoorLayout.MinItemHeight = layout.DoorHeight;
    }

    private void Door_Chosen(object? sender, DoorViewModel door) => ViewModel?.TapDoor(door);

    private void Door_Swinging(object? sender, EventArgs e) => ViewModel?.DoorStartedSwinging();

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
        // A sneak peek saves the confetti for the door's real day.
        if (_confetti is null && ViewModel?.Reveal?.IsSneakPeek != true)
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
