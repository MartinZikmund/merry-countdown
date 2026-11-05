using System.ComponentModel;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Input;

namespace MerryCountdown.Views;

[NavigationInfo(NavigationSection.Countdown)]
public partial class CountdownViewBase : ViewBase<CountdownViewModel> { }

public sealed partial class CountdownView : CountdownViewBase
{
    private const double CompactWidth = 560;
    private const double CompactHeight = 560;
    private const double DoorOffsetRatio = 0.16;
    private const double EdgeMargin = 16;
    private const double DockClearance = 152;
    private const double AboveDock = 92;
    private const double GroundMargin = 20;

    public CountdownView()
    {
        InitializeComponent();
        Overlay.SizeChanged += (_, _) => ArrangeForSize();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            Door.CalmMotion = viewModel.CalmMotion;
        }

        ArrangeForSize();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel is { } viewModel)
        {
            viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CountdownViewModel.IsDoorOnRight) or nameof(CountdownViewModel.DoorStage) or nameof(CountdownViewModel.CalmMotion))
        {
            Door.CalmMotion = ViewModel?.CalmMotion ?? false;
            ArrangeForSize();
        }
    }

    private void ArrangeForSize()
    {
        var (width, height) = (Overlay.ActualWidth, Overlay.ActualHeight);
        var isCompact = width < CompactWidth || height < CompactHeight;
        var stage = ViewModel?.DoorStage ?? HomeDoorStage.Hidden;

        SignControl.IsCompact = isCompact;
        LookBody.Orientation = isCompact ? Orientation.Vertical : Orientation.Horizontal;
        LookOptions.Width = isCompact ? 304 : 460;

        // Before December on a phone the padlocked door would only cover the scene, so the letter button goes alone.
        Door.Visibility = stage == HomeDoorStage.Hidden || (isCompact && stage == HomeDoorStage.Locked) ? Visibility.Collapsed : Visibility.Visible;
        Door.DoorWidth = isCompact ? 95 : 104;

        var onRight = !isCompact && ViewModel?.IsDoorOnRight == true;
        var bottom = isCompact && stage == HomeDoorStage.Locked ? AboveDock : GroundMargin;
        var offset = isCompact ? EdgeMargin : Math.Max(EdgeMargin, width * DoorOffsetRatio);
        DoorSpot.HorizontalAlignment = onRight ? HorizontalAlignment.Right : HorizontalAlignment.Left;
        DoorSpot.Margin = onRight
            ? new Thickness(0, 0, Math.Max(DockClearance, offset), bottom)
            : new Thickness(offset, 0, 0, bottom);
    }

    private void Scene_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel?.SceneTapped();

    private void Door_Swinging(object? sender, EventArgs e) => ViewModel?.DoorStartedSwinging();

    private void Door_LockedTapped(object? sender, EventArgs e) => ViewModel?.LockedDoorTapped();

    private void SceneOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SceneOptionViewModel option })
        {
            SceneFlyout.Hide();
            ViewModel?.SelectSceneCommand.Execute(option);
        }
    }

    // Stays open on purpose, so you can see each sign on the scene behind the flyout.
    private void SignOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SignOptionViewModel option })
        {
            ViewModel?.SelectSignCommand.Execute(option);
        }
    }

    private void StyleOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: StyleOptionViewModel option })
        {
            ViewModel?.SelectStyleCommand.Execute(option);
        }
    }
}
