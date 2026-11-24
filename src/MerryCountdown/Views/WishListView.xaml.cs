using System.ComponentModel;
using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;
using Windows.System;
using Windows.UI;
#if __ANDROID__ || __IOS__
using Windows.UI.ViewManagement;
#endif

namespace MerryCountdown.Views;

[NavigationInfo(NavigationSection.Wishes)]
public partial class WishListViewBase : ViewBase<WishListViewModel> { }

public sealed partial class WishListView : WishListViewBase
{
    private static readonly Color ArrivalGlow = Color.FromArgb(0xFF, 0xFF, 0xE3, 0x9A);

    private bool _wasSent;
    private bool _isNarrow;

    public WishListView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ApplyLayout(LayoutStates.CurrentState == NarrowState);
            WatchKeyboard(true);
            if (ViewModel is not null)
            {
                _wasSent = ViewModel.IsSent;
                ViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
        Unloaded += (_, _) =>
        {
            WatchKeyboard(false);
            if (ViewModel is not null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            // The kid picker outlives this page, and its x:Bind subscription would keep the view model alive with it.
            Bindings.StopTracking();
        };
    }

    private void LayoutStates_CurrentStateChanged(object sender, VisualStateChangedEventArgs e) => ApplyLayout(e.NewState == NarrowState);

    // A visual state can't move an element to another panel: on a phone, sending lives at the end of the letter.
    private void ApplyLayout(bool narrow)
    {
        _isNarrow = narrow;
        Panel target = narrow ? LetterStack : Tools;
        if (FinishPanel.Parent != target)
        {
            ((Panel)FinishPanel.Parent).Children.Remove(FinishPanel);
            target.Children.Add(FinishPanel);
        }
    }

    private void EmptyLetterInvite_Click(object sender, RoutedEventArgs e) => PicturePicker.Open();

    private void RemoveWish_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WishItemViewModel wish })
        {
            ViewModel?.RemoveWishCommand.Execute(wish);
        }
    }

    private void NewWish_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.Enter && ViewModel?.AddWishCommand.CanExecute(null) == true)
        {
            ViewModel.AddWishCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (e.PropertyName == nameof(WishListViewModel.ArrivedWish) && ViewModel.ArrivedWish is { } arrived)
        {
            ShowArrival(arrived);
        }
        else if (e.PropertyName == nameof(WishListViewModel.IsSent))
        {
            if (ViewModel.IsSent && !_wasSent)
            {
                FlyLetterAway();
            }

            _wasSent = ViewModel.IsSent;
        }
    }

    // Scroll the new line into view and let it pop in. Calm motion keeps the glow and drops the movement.
    private void ShowArrival(WishItemViewModel wish)
    {
        var index = ViewModel!.Wishes.IndexOf(wish);
        if (index < 0)
        {
            return;
        }

        var calm = ViewModel.CalmMotion;
        WishesRepeater.UpdateLayout();
        if (WishesRepeater.GetOrCreateElement(index) is not Grid line)
        {
            return;
        }

        line.UpdateLayout();
        line.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = !calm, VerticalAlignmentRatio = 0.5 });

        SolidColorBrush glow = new(ArrivalGlow);
        line.Background = glow;
        Storyboard arrival = new();
        ColorAnimation fade = new()
        {
            To = Colors.Transparent,
            BeginTime = TimeSpan.FromMilliseconds(600),
            Duration = TimeSpan.FromMilliseconds(1200),
        };
        Storyboard.SetTarget(fade, glow);
        Storyboard.SetTargetProperty(fade, nameof(SolidColorBrush.Color));
        arrival.Children.Add(fade);

        if (!calm && line.RenderTransform is CompositeTransform transform)
        {
            Pop(transform, nameof(CompositeTransform.ScaleX));
            Pop(transform, nameof(CompositeTransform.ScaleY));
        }

        arrival.Begin();

        void Pop(CompositeTransform transform, string property)
        {
            DoubleAnimation pop = new()
            {
                From = 0.4,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(420),
                EasingFunction = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 },
            };
            Storyboard.SetTarget(pop, transform);
            Storyboard.SetTargetProperty(pop, property);
            arrival.Children.Add(pop);
        }
    }

    private void WatchKeyboard(bool watch)
    {
#if __ANDROID__ || __IOS__
        var pane = InputPane.GetForCurrentView();
        pane.Showing -= OnKeyboardShowing;
        pane.Hiding -= OnKeyboardHiding;
        if (watch)
        {
            pane.Showing += OnKeyboardShowing;
            pane.Hiding += OnKeyboardHiding;
        }
#endif
    }

#if __ANDROID__ || __IOS__
    private void OnKeyboardShowing(InputPane sender, InputPaneVisibilityEventArgs args) => MakeRoomForKeyboard(args.OccludedRect.Height);

    private void OnKeyboardHiding(InputPane sender, InputPaneVisibilityEventArgs args) => MakeRoomForKeyboard(0);
#endif

    // The keyboard also covers the tab bar under the page, so lift the page only by what it hides of the page itself.
    // While it's up on a phone, the picture button folds away to leave room for the letter.
    private void MakeRoomForKeyboard(double keyboardHeight)
    {
        var below = XamlRoot is { } root ? root.Size.Height - TransformToVisual(null).TransformPoint(new Point(0, ActualHeight)).Y : 0;
        LayoutRoot.Margin = new Thickness(0, 0, 0, Math.Max(0, keyboardHeight - below));
        PicturePicker.Visibility = keyboardHeight > 0 && _isNarrow ? Visibility.Collapsed : Visibility.Visible;
    }

    // Off to the North Pole, or wherever Ježíšek lives, and back into view for reading later.
    private void FlyLetterAway()
    {
        Storyboard flight = new();
        Add(flight, nameof(CompositeTransform.TranslateY), [0, -24, -900, 60, 0]);
        Add(flight, nameof(CompositeTransform.ScaleX), [1, 1.04, 0.2, 0.9, 1]);
        Add(flight, nameof(CompositeTransform.ScaleY), [1, 1.04, 0.2, 0.9, 1]);
        Add(flight, nameof(CompositeTransform.Rotation), [0, -2, 12, -1, 0]);
        flight.Begin();

        void Add(Storyboard storyboard, string property, double[] values)
        {
            int[] times = [0, 180, 800, 1120, 1320];
            DoubleAnimationUsingKeyFrames animation = new();
            for (var i = 0; i < values.Length; i++)
            {
                animation.KeyFrames.Add(new EasingDoubleKeyFrame
                {
                    KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(times[i])),
                    Value = values[i],
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut },
                });
            }

            Storyboard.SetTarget(animation, LetterTransform);
            Storyboard.SetTargetProperty(animation, property);
            storyboard.Children.Add(animation);
        }
    }
}
