using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Views;

public partial class MainViewBase : ViewBase<MainViewModel> { }

[NavigationInfo(NavigationSection.Main)]
public sealed partial class MainView : MainViewBase
{
    public MainView()
    {
        this.InitializeComponent();
    }
}
