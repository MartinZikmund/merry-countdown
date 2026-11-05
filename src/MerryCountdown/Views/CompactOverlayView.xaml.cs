using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace MerryCountdown.Views;

/// <summary>The compact overlay. A plain control, not a page: like the Family Display, it covers the whole shell.</summary>
public sealed partial class CompactOverlayView : UserControl
{
    public CompactOverlayView(CompactOverlayViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        Loaded += (_, _) =>
        {
            ViewModel.Start();
            Focus(FocusState.Programmatic);
        };
        Unloaded += (_, _) => ViewModel.Stop();
    }

    public CompactOverlayViewModel ViewModel { get; }

    private void Scene_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel.SceneTapped();

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Escape or VirtualKey.GoBack)
        {
            ViewModel.ExitCommand.Execute(null);
            e.Handled = true;
        }
    }
}
