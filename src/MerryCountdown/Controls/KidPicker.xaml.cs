using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Controls;

public sealed partial class KidPicker : UserControl
{
    public static readonly DependencyProperty PickerProperty = DependencyProperty.Register(
        nameof(Picker), typeof(KidPickerViewModel), typeof(KidPicker), new PropertyMetadata(null, (d, e) => ((KidPicker)d).Repeater.ItemsSource = (e.NewValue as KidPickerViewModel)?.Kids));

    public KidPicker() => InitializeComponent();

    public KidPickerViewModel? Picker
    {
        get => (KidPickerViewModel?)GetValue(PickerProperty);
        set => SetValue(PickerProperty, value);
    }
}
