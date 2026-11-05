using System.Windows.Input;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Animation;

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

    private const double OpenedLeafScale = 0.12;

    private bool _isOpening;

    public HomeDoor()
    {
        InitializeComponent();
        AutomationProperties.SetAutomationId(DoorButton, "TodaysDoor");
        Apply();
    }

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
            DoorScale.Width = value;
            DoorScale.Height = value * 312 / 216;
        }
    }

    private void Apply()
    {
        var locked = Stage == HomeDoorStage.Locked;

        LeafFace.Background = SignColors.Brush(locked ? 0x5D4A55u : 0x94283Fu);
        UpperPanel.BorderBrush = SignColors.Brush(locked ? 0x4B3A45u : 0x7B1F33u);
        LowerPanel.BorderBrush = UpperPanel.BorderBrush;
        Wreath.Stroke = SignColors.Brush(locked ? 0x4E6155u : 0x1B5A45u);
        NumberText.Text = Number.ToString(System.Globalization.CultureInfo.CurrentCulture);
        Knob.Visibility = locked ? Visibility.Collapsed : Visibility.Visible;
        Padlock.Visibility = locked ? Visibility.Visible : Visibility.Collapsed;

        if (!_isOpening)
        {
            LeafScale.ScaleX = Stage == HomeDoorStage.Opened ? OpenedLeafScale : 1;
        }
    }

    private void OnClick(object sender, RoutedEventArgs e)
    {
        switch (Stage)
        {
            case HomeDoorStage.Waiting when !CalmMotion:
                PlayOpening();
                break;
            case HomeDoorStage.Waiting or HomeDoorStage.Opened:
                Command?.Execute(null);
                break;
            case HomeDoorStage.Locked when !CalmMotion:
                PlayJiggle();
                break;
        }
    }

    private void PlayOpening()
    {
        if (_isOpening)
        {
            return;
        }

        _isOpening = true;
        DoubleAnimation swing = new()
        {
            To = OpenedLeafScale,
            Duration = new Duration(TimeSpan.FromMilliseconds(600)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(swing, LeafScale);
        Storyboard.SetTargetProperty(swing, nameof(LeafScale.ScaleX));

        Storyboard storyboard = new();
        storyboard.Children.Add(swing);
        storyboard.Completed += (_, _) =>
        {
            _isOpening = false;
            Command?.Execute(null);
        };
        storyboard.Begin();
    }

    private void PlayJiggle()
    {
        DoubleAnimationUsingKeyFrames shake = new();
        foreach (var (ms, x) in new (double Ms, double X)[] { (0, 0), (70, -8), (140, 8), (210, -5), (280, 0) })
        {
            shake.KeyFrames.Add(new LinearDoubleKeyFrame { KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ms)), Value = x });
        }

        Storyboard.SetTarget(shake, Jiggle);
        Storyboard.SetTargetProperty(shake, nameof(Jiggle.X));
        Storyboard storyboard = new();
        storyboard.Children.Add(shake);
        storyboard.Begin();
    }
}
