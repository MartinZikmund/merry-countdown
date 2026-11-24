using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Input;

namespace MerryCountdown.Views;

[NavigationInfo(NavigationSection.Countdown)]
public partial class CountdownViewBase : ViewBase<CountdownViewModel> { }

public sealed partial class CountdownView : CountdownViewBase
{
    private const double CompactSize = 560;

    public CountdownView()
    {
        InitializeComponent();
        Root.SizeChanged += (_, _) =>
        {
            SignControl.IsCompact = Root.ActualWidth < CompactSize || Root.ActualHeight < CompactSize;
            TellSceneAboutSign();
        };
        SignBox.SizeChanged += (_, _) => TellSceneAboutSign();

        // The backdrop outlives this view, and its x:Bind subscription would keep the view model alive with it.
        Unloaded += (_, _) => Bindings.StopTracking();
    }

    private void TellSceneAboutSign() =>
        Scene.CoveredTop = Root.ActualHeight > 0 ? SignBox.ActualHeight / Root.ActualHeight : 0;

    private void Scene_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel?.SceneTapped();
}
