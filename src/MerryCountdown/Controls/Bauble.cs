using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace MerryCountdown.Controls;

/// <summary>A number hanging on its own bauble, for the garland sign.</summary>
[ContentProperty(Name = nameof(Body))]
public sealed partial class Bauble : StackPanel
{
    private const double CapHeight = 10;

    private readonly Rectangle _string = new() { Width = 2, Fill = SignColors.Brush(0xFFE3A1) };
    private readonly Ellipse _ball = new();
    private readonly Grid _ballHost = new();
    private readonly RotateTransform _swing = new();
    private UIElement? _body;

    public Bauble()
    {
        RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0);
        RenderTransform = _swing;

        Ellipse shine = new()
        {
            Width = 28,
            Height = 16,
            Margin = new Thickness(24, 14, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Fill = SignColors.Brush(0xFFFFFF, 0x44),
            RenderTransformOrigin = new Windows.Foundation.Point(0.5, 0.5),
            RenderTransform = new RotateTransform { Angle = -28 },
        };
        _ballHost.Children.Add(_ball);
        _ballHost.Children.Add(shine);

        Children.Add(_string);
        Children.Add(new Rectangle { Width = 18, Height = CapHeight, Fill = SignColors.Brush(0xD9AE52), RadiusX = 3, RadiusY = 3 });
        Children.Add(_ballHost);
    }

    public RotateTransform Swing => _swing;

    public double BallSize
    {
        get => _ballHost.Width;
        set
        {
            _ballHost.Width = value;
            _ballHost.Height = value;
        }
    }

    public double StringLength
    {
        get => _string.Height;
        set => _string.Height = value;
    }

    public Brush? Fill
    {
        get => _ball.Fill;
        set => _ball.Fill = value;
    }

    public UIElement? Body
    {
        get => _body;
        set
        {
            if (_body is not null)
            {
                _ballHost.Children.Remove(_body);
            }

            _body = value;
            if (_body is not null)
            {
                _ballHost.Children.Add(_body);
            }
        }
    }
}
