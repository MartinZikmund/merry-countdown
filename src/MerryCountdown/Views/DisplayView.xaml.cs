using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Input;
using Windows.System;

namespace MerryCountdown.Views;

/// <summary>The Family Display. A plain control, not a page: it covers the whole shell, navigation included.</summary>
public sealed partial class DisplayView : UserControl
{
    public DisplayView(DisplayViewModel viewModel)
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

    public DisplayViewModel ViewModel { get; }

    private void Scene_Tapped(object sender, TappedRoutedEventArgs e) => ViewModel.SceneTapped();

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e) => ViewModel.ShowHint();

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        ViewModel.ShowHint();

        switch (e.Key)
        {
            case VirtualKey.Escape or VirtualKey.GoBack or VirtualKey.GamepadB:
                ViewModel.ExitCommand.Execute(null);
                e.Handled = true;
                break;
            case VirtualKey.Right or VirtualKey.Left or VirtualKey.GamepadDPadRight or VirtualKey.GamepadDPadLeft or VirtualKey.Space:
                ViewModel.NextScene();
                e.Handled = true;
                break;
        }
    }
}
