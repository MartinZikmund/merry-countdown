using System.ComponentModel;
using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.System;

namespace MerryCountdown.Views;

[NavigationInfo(NavigationSection.Wishes)]
public partial class WishListViewBase : ViewBase<WishListViewModel> { }

public sealed partial class WishListView : WishListViewBase
{
    private bool _wasSent;

    public WishListView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            if (ViewModel is not null)
            {
                _wasSent = ViewModel.IsSent;
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

    private void RemoveWish_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WishItemViewModel wish })
        {
            ViewModel?.RemoveWishCommand.Execute(wish);
        }
    }

    private void Sticker_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton { Tag: WishStickerViewModel sticker } radio && ViewModel is { } viewModel)
        {
            viewModel.ChooseStickerCommand.Execute(sticker);
            radio.IsChecked = viewModel.SelectedSticker == sticker;
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
        if (e.PropertyName != nameof(WishListViewModel.IsSent) || ViewModel is null)
        {
            return;
        }

        if (ViewModel.IsSent && !_wasSent)
        {
            FlyLetterAway();
        }

        _wasSent = ViewModel.IsSent;
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
