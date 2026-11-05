using MerryCountdown.Core.Navigation;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Views;

[NavigationInfo(NavigationSection.Countdown)]
public partial class CountdownViewBase : ViewBase<CountdownViewModel> { }

public sealed partial class CountdownView : CountdownViewBase
{
    public CountdownView() => InitializeComponent();

    private void SceneOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SceneOptionViewModel option })
        {
            SceneFlyout.Hide();
            ViewModel?.SelectSceneCommand.Execute(option);
        }
    }
}
