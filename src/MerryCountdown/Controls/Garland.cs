using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace MerryCountdown.Controls;

/// <summary>A pine garland along the top edge. Grown in big steps so dragging a window doesn't redraw it every pixel.</summary>
public sealed partial class Garland : Grid
{
    private const double GrowthStep = 1024;
    private const double BandHeight = 36;
    private const double NeedleSpacing = 7;
    private const double LightSpacing = 82;
    private static readonly uint[] Greens = [0x123F30, 0x176A4E, 0x1F7D5C, 0x0E3226];
    private static readonly uint[] LightColors = [0xFFC95C, 0xFF9CAD, 0x9CE0FF];

    private double _builtWidth;

    public Garland()
    {
        Height = BandHeight;
        IsHitTestVisible = false;
        SizeChanged += (_, e) => Grow(e.NewSize.Width);
    }

    private void Grow(double width)
    {
        if (width <= _builtWidth)
        {
            return;
        }

        _builtWidth = Math.Ceiling(width / GrowthStep) * GrowthStep;
        Children.Clear();

        Random random = new(7);
        var needles = Greens.Select(_ => new PathGeometry()).ToArray();
        for (var x = -NeedleSpacing; x < _builtWidth + NeedleSpacing; x += NeedleSpacing)
        {
            for (var i = 0; i < 4; i++)
            {
                var angle = (random.NextDouble() * Math.PI * 1.3) - 0.15;
                var length = 10 + (random.NextDouble() * 12);
                var y = 8 + (random.NextDouble() * 8);

                PathFigure needle = new() { StartPoint = new(x, y) };
                needle.Segments.Add(new LineSegment { Point = new(x + (Math.Cos(angle) * length), y + (Math.Sin(angle) * length)) });
                needles[random.Next(needles.Length)].Figures.Add(needle);
            }
        }

        for (var i = 0; i < needles.Length; i++)
        {
            Children.Add(new Microsoft.UI.Xaml.Shapes.Path
            {
                Data = needles[i],
                Stroke = SignColors.Brush(Greens[i]),
                StrokeThickness = 2.2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            });
        }

        var index = 0;
        for (var x = 30.0; x < _builtWidth; x += LightSpacing, index++)
        {
            Children.Add(new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = SignColors.Brush(LightColors[index % LightColors.Length]),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(x, 10 + ((index % 2) * 6), 0, 0),
            });
        }
    }
}
