using System.Windows.Input;
using MerryCountdown.Core.ViewModels;
using MerryCountdown.Services.Localization;

namespace MerryCountdown.Controls;

/// <summary>
/// A button that shows the chosen picture and opens a flyout with all of them, grouped by kind.
/// Picking one runs <see cref="ChooseCommand"/> with the sticker and closes the flyout.
/// </summary>
public sealed partial class StickerPicker : UserControl
{
    private const double FlyoutMaxWidth = 336;
    private const double FlyoutMargin = 48;

    public static readonly DependencyProperty GroupsProperty = DependencyProperty.Register(
        nameof(Groups), typeof(IEnumerable<WishStickerGroupViewModel>), typeof(StickerPicker), new PropertyMetadata(null, (d, e) => ((StickerPicker)d).GroupsRepeater.ItemsSource = e.NewValue));

    public static readonly DependencyProperty SelectedStickerProperty = DependencyProperty.Register(
        nameof(SelectedSticker), typeof(WishStickerViewModel), typeof(StickerPicker), new PropertyMetadata(null, (d, _) => ((StickerPicker)d).ShowSelection()));

    public static readonly DependencyProperty ChooseCommandProperty = DependencyProperty.Register(
        nameof(ChooseCommand), typeof(ICommand), typeof(StickerPicker), new PropertyMetadata(null));

    public StickerPicker() => InitializeComponent();

    public IEnumerable<WishStickerGroupViewModel>? Groups
    {
        get => (IEnumerable<WishStickerGroupViewModel>?)GetValue(GroupsProperty);
        set => SetValue(GroupsProperty, value);
    }

    public WishStickerViewModel? SelectedSticker
    {
        get => (WishStickerViewModel?)GetValue(SelectedStickerProperty);
        set => SetValue(SelectedStickerProperty, value);
    }

    public ICommand? ChooseCommand
    {
        get => (ICommand?)GetValue(ChooseCommandProperty);
        set => SetValue(ChooseCommandProperty, value);
    }

    private void ShowSelection()
    {
        var sticker = SelectedSticker;
        InviteIcon.Visibility = sticker is null ? Visibility.Visible : Visibility.Collapsed;
        ChosenImage.Visibility = sticker is null ? Visibility.Collapsed : Visibility.Visible;
        ChosenImage.Source = sticker is null ? null : Stickers.Image(sticker.Key);
        Caption.Text = sticker?.Name ?? Localizer.Instance.GetString("WishPickPictureButton");
        AutomationProperties.SetName(PickerButton, Caption.Text);
    }

    // Four 64-wide pictures fit a 320 phone, so cap the flyout at the window minus its margins.
    private void Flyout_Opening(object? sender, object e)
    {
        var width = XamlRoot is { } root ? Math.Min(FlyoutMaxWidth, root.Size.Width - FlyoutMargin) : FlyoutMaxWidth;
        FlyoutScroller.Width = Math.Max(width, 64);
    }

    private void Sticker_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: WishStickerViewModel sticker })
        {
            ChooseCommand?.Execute(sticker);
            PickerFlyout.Hide();
        }
    }
}
