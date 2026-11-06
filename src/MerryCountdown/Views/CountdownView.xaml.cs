using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Input;

namespace MerryCountdown.Views;

[NavigationInfo(NavigationSection.Countdown)]
public partial class CountdownViewBase : ViewBase<CountdownViewModel> { }

public sealed partial class CountdownView : CountdownViewBase
{
    private const double CompactSize = 560;
    private const double ControlsMargin = 16;
    private const double ControlsGapUnderSign = 8;

    public CountdownView()
    {
        InitializeComponent();
        Root.SizeChanged += (_, _) => ArrangeForSize();
        SkyPanel.SizeChanged += (_, _) => ArrangeForSize();
        SceneControls.SizeChanged += (_, _) => ArrangeForSize();
    }

    private void ArrangeForSize()
    {
        var isCompact = Root.ActualWidth < CompactSize || Root.ActualHeight < CompactSize;

        SignControl.IsCompact = isCompact;
        LookBody.Orientation = isCompact ? Orientation.Vertical : Orientation.Horizontal;
        LookOptions.Width = isCompact ? 304 : 460;

        // Signs differ in width, so measure: beside the sign if they fit, otherwise under it.
        var roomBesideSign = (Root.ActualWidth - SkyPanel.ActualWidth) / 2;
        var fitsBeside = roomBesideSign >= SceneControls.ActualWidth + (2 * ControlsMargin);
        Grid.SetRow(SceneControls, fitsBeside ? 0 : 1);
        SceneControls.Margin = new Thickness(0, fitsBeside ? ControlsMargin : ControlsGapUnderSign, ControlsMargin, 0);
    }

    private void Scene_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel?.SceneTapped();

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
