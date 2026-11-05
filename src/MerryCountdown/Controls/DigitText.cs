using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace MerryCountdown.Controls;

public sealed record DigitLook(
    double FontSize,
    double CellWidth,
    double CellHeight,
    Brush Ink,
    Brush? Plate = null,
    double Corner = 0,
    double Gap = 0,
    Brush? Hinge = null,
    Brush? Perforation = null);

/// <summary>
/// A number with one fixed cell per character. Baloo's "1" is much narrower than its "0",
/// so plain centred text shuffles sideways on every tick. Cells can't.
/// </summary>
public sealed partial class DigitText : StackPanel
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(DigitText), new PropertyMetadata("", (d, _) => ((DigitText)d).Sync()));

    private static readonly Duration FlipDuration = new(TimeSpan.FromMilliseconds(240));

    private readonly List<(TextBlock Digit, ScaleTransform Scale)> _cells = [];
    private DigitLook? _look;

    public DigitText()
    {
        Orientation = Orientation.Horizontal;
        AutomationProperties.SetAccessibilityView(this, AccessibilityView.Raw);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool Animate { get; set; }

    public DigitLook? Look
    {
        get => _look;
        set
        {
            if (_look != value)
            {
                _look = value;
                Rebuild();
            }
        }
    }

    private void Sync()
    {
        var text = Text ?? "";
        if (_look is null)
        {
            return;
        }

        if (text.Length != _cells.Count)
        {
            Rebuild();
            return;
        }

        for (var i = 0; i < text.Length; i++)
        {
            var value = text[i].ToString();
            var (digit, scale) = _cells[i];
            if (digit.Text != value)
            {
                digit.Text = value;
                if (Animate)
                {
                    Flip(scale);
                }
            }
        }
    }

    private void Rebuild()
    {
        Children.Clear();
        _cells.Clear();

        if (_look is not { } look)
        {
            return;
        }

        Spacing = look.Gap;
        foreach (var character in Text ?? "")
        {
            Children.Add(CreateCell(look, character.ToString()));
        }
    }

    private Grid CreateCell(DigitLook look, string value)
    {
        Grid cell = new()
        {
            Width = look.CellWidth,
            Height = look.CellHeight,
            Background = look.Plate,
            CornerRadius = new CornerRadius(look.Corner),
        };

        if (look.Hinge is { } hinge)
        {
            cell.Children.Add(new Rectangle { Height = 2, Fill = hinge, VerticalAlignment = VerticalAlignment.Center });
        }

        ScaleTransform scale = new();
        TextBlock digit = new()
        {
            FontFamily = (FontFamily)Application.Current.Resources["DisplayFontFamily"],
            FontSize = look.FontSize,
            Foreground = look.Ink,
            Text = value,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
            RenderTransform = scale,
        };
        cell.Children.Add(digit);

        if (look.Perforation is { } perforation)
        {
            cell.Children.Add(new Rectangle
            {
                Stroke = perforation,
                StrokeThickness = 4,
                StrokeDashCap = PenLineCap.Round,
                StrokeDashArray = [0, 1.75],
            });
        }

        _cells.Add((digit, scale));
        return cell;
    }

    private static void Flip(ScaleTransform scale)
    {
        DoubleAnimation animation = new()
        {
            From = 0,
            To = 1,
            Duration = FlipDuration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
        };
        Storyboard.SetTarget(animation, scale);
        Storyboard.SetTargetProperty(animation, nameof(ScaleTransform.ScaleY));

        Storyboard storyboard = new();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }
}
