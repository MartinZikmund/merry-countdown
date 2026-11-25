using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Controls;

/// <summary>What the app bar's customize button opens on Home: the two display modes, then the sign and the scene.</summary>
public sealed partial class LookPicker : UserControl
{
    private const double CompactSize = 560;

    public LookPicker(CountdownViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
        Loaded += (_, _) => ArrangeForWindow();
    }

    public CountdownViewModel ViewModel { get; }

    /// <summary>Raised by the choices that take over the window or open a dialog. Picking a sign doesn't: you watch it change behind the flyout.</summary>
    public event EventHandler? CloseRequested;

    // Runs on every opening, because the window may have been resized since the last one.
    private void ArrangeForWindow()
    {
        var window = XamlRoot?.Size ?? default;
        var isCompact = window.Width < CompactSize || window.Height < CompactSize;

        Modes.Orientation = isCompact ? Orientation.Vertical : Orientation.Horizontal;
        LookBody.Orientation = isCompact ? Orientation.Vertical : Orientation.Horizontal;
        LookOptions.Width = isCompact ? 304 : 460;
    }

    private void FamilyDisplay_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
        ViewModel.EnterFamilyDisplayCommand.Execute(null);
    }

    private void CompactOverlay_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
        ViewModel.EnterCompactOverlayCommand.Execute(null);
    }

    private async void PhotosToggle_Toggled(object sender, RoutedEventArgs e)
    {
        // The binding setting the switch lands here too.
        var isOn = PhotosToggle.IsOn;
        if (isOn == ViewModel.PhotosBehindScene)
        {
            return;
        }

        if (isOn && ViewModel.PhotosNeedSetup)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        await ViewModel.SetPhotosBehindSceneAsync(isOn);
        PhotosToggle.IsOn = ViewModel.PhotosBehindScene;
    }

    private void SceneOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SceneOptionViewModel option })
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            ViewModel.SelectSceneCommand.Execute(option);
        }
    }

    private void SignOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: SignOptionViewModel option })
        {
            ViewModel.SelectSignCommand.Execute(option);
        }
    }

    private void StyleOption_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: StyleOptionViewModel option })
        {
            ViewModel.SelectStyleCommand.Execute(option);
        }
    }
}
