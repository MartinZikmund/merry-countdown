using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Views;

public partial class SettingsViewBase : ViewBase<SettingsViewModel> { }

[NavigationInfo(NavigationSection.Settings)]
public sealed partial class SettingsView : SettingsViewBase
{
    public SettingsView()
    {
        this.InitializeComponent();
    }

    public static Visibility VisibleWhenSet(string? text) =>
        string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
}
