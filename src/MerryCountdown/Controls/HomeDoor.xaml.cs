using System.Windows.Input;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;

namespace MerryCountdown.Controls;

/// <summary>Today's door, standing small on the ground in the scene. It swings open in place before the surprise.</summary>
public sealed partial class HomeDoor : UserControl
{
    public static readonly DependencyProperty StageProperty = DependencyProperty.Register(
        nameof(Stage), typeof(HomeDoorStage), typeof(HomeDoor), new PropertyMetadata(HomeDoorStage.Hidden, (d, _) => ((HomeDoor)d).Apply()));

    public static readonly DependencyProperty NumberProperty = DependencyProperty.Register(
        nameof(Number), typeof(int), typeof(HomeDoor), new PropertyMetadata(1, (d, _) => ((HomeDoor)d).Apply()));

    public static readonly DependencyProperty CaptionProperty = DependencyProperty.Register(
        nameof(Caption), typeof(string), typeof(HomeDoor), new PropertyMetadata("", (d, e) => AutomationProperties.SetName(((HomeDoor)d).DoorButton, (string)e.NewValue)));

    private readonly ImageSource _gift = Stickers.Image("gift");
    private readonly DoorPaint _lockedPaint;
    private readonly DoorPaint _openablePaint;

    public HomeDoor()
    {
        InitializeComponent();
        AutomationProperties.SetAutomationId(DoorButton, "TodaysDoor");

        var gold = (Brush)Application.Current.Resources["GoldBrush"];
        var frame = SignColors.Brush(0xCDBF9F);
        var inside = SignColors.Brush(0xE8A84C);
        _lockedPaint = new(SignColors.Brush(0x5D4A55), SignColors.Brush(0x4B3A45), SignColors.Brush(0x4E6155), gold, frame, inside);
        _openablePaint = new(SignColors.Brush(0x94283F), SignColors.Brush(0x7B1F33), SignColors.Brush(0x1B5A45), gold, frame, inside);
        Apply();
    }

    public event EventHandler? Swinging;

    public event EventHandler? LockedTapped;

    public HomeDoorStage Stage
    {
        get => (HomeDoorStage)GetValue(StageProperty);
        set => SetValue(StageProperty, value);
    }

    public int Number
    {
        get => (int)GetValue(NumberProperty);
        set => SetValue(NumberProperty, value);
    }

    public string Caption
    {
        get => (string)GetValue(CaptionProperty);
        set => SetValue(CaptionProperty, value);
    }

    public bool CalmMotion { get; set; }

    public ICommand? Command { get; set; }

    public double DoorWidth
    {
        set
        {
            Art.Width = value;
            Art.Height = value * 312 / 216;
        }
    }

    private void Apply()
    {
        var locked = Stage == HomeDoorStage.Locked;
        Art.Show(Number, locked, Stage == HomeDoorStage.Opened, _gift, locked ? _lockedPaint : _openablePaint);
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        switch (Stage)
        {
            case HomeDoorStage.Waiting when !CalmMotion:
                Swinging?.Invoke(this, EventArgs.Empty);
                Art.SwingOpen(() => Command?.Execute(null));
                break;
            case HomeDoorStage.Waiting or HomeDoorStage.Opened:
                Command?.Execute(null);
                break;
            case HomeDoorStage.Locked:
                LockedTapped?.Invoke(this, EventArgs.Empty);
                if (!CalmMotion)
                {
                    Art.Shake();
                }

                break;
        }
    }
}
