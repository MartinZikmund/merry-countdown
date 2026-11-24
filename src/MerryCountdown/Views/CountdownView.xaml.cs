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
        Root.SizeChanged += (_, _) => SignControl.IsCompact = Root.ActualWidth < CompactSize || Root.ActualHeight < CompactSize;
    }

    private void Scene_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel?.SceneTapped();
}
