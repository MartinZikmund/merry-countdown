using System.ComponentModel;
using System.Diagnostics;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Services;
using MerryCountdown.Core.ViewModels;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Shapes;

namespace MerryCountdown.Controls;

public sealed partial class CountdownSign : UserControl
{
    public static readonly DependencyProperty SignProperty = DependencyProperty.Register(
        nameof(Sign), typeof(CountdownSignViewModel), typeof(CountdownSign), new PropertyMetadata(null, OnSignChanged));

    public static readonly DependencyProperty IsCompactProperty = DependencyProperty.Register(
        nameof(IsCompact), typeof(bool), typeof(CountdownSign), new PropertyMetadata(false, (d, _) => ((CountdownSign)d).Apply()));

    public static readonly DependencyProperty IsPreviewProperty = DependencyProperty.Register(
        nameof(IsPreview), typeof(bool), typeof(CountdownSign), new PropertyMetadata(false, (d, _) => ((CountdownSign)d).Apply()));

    public static readonly DependencyProperty CalmMotionProperty = DependencyProperty.Register(
        nameof(CalmMotion), typeof(bool), typeof(CountdownSign), new PropertyMetadata(false, (d, _) => ((CountdownSign)d).Apply()));

    // Baloo's widest digit is 0.6 em wide, so a cell of 0.62 em fits every digit with a hair to spare.
    private const double CellPerEm = 0.62;

    private readonly DigitText[] _plateDigits;
    private readonly DigitText[] _clockDigits;
    private readonly TextBlock[] _plateLabels;
    private readonly Bauble[] _baubles;

    public CountdownSign()
    {
        InitializeComponent();
        _plateDigits = [DaysDigits, HoursDigits, MinutesDigits, SecondsDigits];
        _clockDigits = [ClockHoursDigits, ClockMinutesDigits, ClockSecondsDigits];
        _plateLabels = [DaysLabel, HoursLabel, MinutesLabel, SecondsLabel];
        _baubles = [DaysBauble, HoursBauble, MinutesBauble, SecondsBauble, SleepsBauble, ClockHoursBauble, ClockMinutesBauble, ClockSecondsBauble, MerryBauble];
        Apply();
    }

    public CountdownSignViewModel? Sign
    {
        get => (CountdownSignViewModel?)GetValue(SignProperty);
        set => SetValue(SignProperty, value);
    }

    /// <summary>Phones and short windows get the smaller sizes.</summary>
    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
    }

    /// <summary>The little picture in the look flyout: no ropes, no motion, no tapping.</summary>
    public bool IsPreview
    {
        get => (bool)GetValue(IsPreviewProperty);
        set => SetValue(IsPreviewProperty, value);
    }

    public bool CalmMotion
    {
        get => (bool)GetValue(CalmMotionProperty);
        set => SetValue(CalmMotionProperty, value);
    }

    private static void OnSignChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (CountdownSign)d;
        if (e.OldValue is CountdownSignViewModel old)
        {
            old.PropertyChanged -= control.OnSignPropertyChanged;
        }

        if (e.NewValue is CountdownSignViewModel sign)
        {
            sign.PropertyChanged += control.OnSignPropertyChanged;
        }

        control.Apply();
    }

    private void OnSignPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(CountdownSignViewModel.Design)
            or nameof(CountdownSignViewModel.Style)
            or nameof(CountdownSignViewModel.IsCelebrating))
        {
            Apply();
        }
    }

    private void Apply()
    {
        if (_plateDigits is null || Sign is not { } sign)
        {
            return;
        }

        var design = sign.Design;
        var isBaubles = design == SignDesign.Baubles;
        var showPlates = !sign.IsCelebrating && sign.Style == CountdownStyle.Plates;
        var showSleeps = !sign.IsCelebrating && sign.Style == CountdownStyle.SleepsAndClock;

        BoardSign.Visibility = Show(!isBaubles);
        BaubleSign.Visibility = Show(isBaubles);
        PlatesBody.Visibility = Show(showPlates);
        SleepsBody.Visibility = Show(showSleeps);
        BoardMerry.Visibility = Show(sign.IsCelebrating);
        BaublePlates.Visibility = Show(showPlates);
        BaubleSleeps.Visibility = Show(showSleeps);
        MerryBauble.Visibility = Show(sign.IsCelebrating);
        Root.IsHitTestVisible = !IsPreview;

        if (isBaubles)
        {
            ApplyBaubles();
        }
        else
        {
            ApplyBoard(design);
        }
    }

    private void ApplyBoard(SignDesign design)
    {
        var look = SignLook.For(design);
        var size = IsCompact ? BoardSize.Compact : BoardSize.Regular;
        var animate = !CalmMotion && !IsPreview;

        PlankFace.Visibility = Show(design == SignDesign.Plank);
        SnowCap.Visibility = Show(design == SignDesign.Plank);
        BiscuitFill.Visibility = Show(design == SignDesign.Gingerbread);
        IcingLine.Visibility = Show(design == SignDesign.Gingerbread);
        TagPaper.Visibility = Show(design == SignDesign.ParcelTag);
        Eyelet.Visibility = Show(design == SignDesign.ParcelTag);
        StationFace.Visibility = Show(design == SignDesign.Station);

        Hanger.Visibility = Show(!IsPreview);
        HangLeft.Visibility = Show(look.HangerWidth > 0 && !look.SingleString);
        HangRight.Visibility = HangLeft.Visibility;
        HangCentre.Visibility = Show(look.SingleString);
        foreach (var rope in new[] { HangLeft, HangRight, HangCentre })
        {
            rope.Width = look.HangerWidth;
            rope.Fill = look.Hanger;
        }

        BoardBody.Padding = look.Padding;
        PlatesBody.Spacing = size.GroupGap;

        DigitLook plate = new(size.PlateFont, size.PlateWidth, size.PlateHeight, look.PlateInk, look.PlateFill, look.PlateCorner, 2, look.Hinge, look.Perforation);
        foreach (var digits in _plateDigits.Concat(_clockDigits))
        {
            digits.Look = plate;
            digits.Animate = animate;
        }

        var labelFont = look.UseHandwriting ? (FontFamily)Application.Current.Resources["HandwritingFontFamily"] : DisplayFont;
        foreach (var label in _plateLabels)
        {
            Text(label, labelFont, size.LabelSize, look.Label);
            label.HorizontalAlignment = HorizontalAlignment.Center;
        }

        SleepsDigits.Look = new DigitLook(size.BigFont, size.BigFont * CellPerEm, size.BigFont * 0.95, look.Big);
        SleepsDigits.Animate = animate;
        Text(SleepsWord, labelFont, size.BigFont * 0.38, look.Label);
        Text(ColonOne, DisplayFont, size.PlateFont * 0.8, look.Label);
        Text(ColonTwo, DisplayFont, size.PlateFont * 0.8, look.Label);
        Text(NextChange, labelFont, size.LabelSize * 1.1, look.Label);
        Text(BoardMerry, DisplayFont, size.MerryFont, look.Big);
    }

    private void ApplyBaubles()
    {
        var compact = IsCompact;
        var animate = !CalmMotion && !IsPreview;
        var ball = compact ? 82.0 : 128.0;
        var font = compact ? 32.0 : 48.0;
        var label = compact ? 12.0 : 15.0;
        var big = compact ? 128.0 : 172.0;
        var bigFont = compact ? 58.0 : 80.0;
        var small = compact ? 62.0 : 84.0;
        var smallFont = compact ? 26.0 : 36.0;

        BaublePlates.Spacing = compact ? 4 : 10;
        BaubleSleeps.Spacing = compact ? 4 : 10;
        ClockBalls.Spacing = compact ? 3 : 6;

        var white = SignColors.Brush(0xFFFFFF);
        var navy = SignColors.Brush(0x2A2F55);
        foreach (var (digits, ink, size) in new[]
        {
            (DaysBall, white, font), (HoursBall, navy, font), (MinutesBall, white, font), (SecondsBall, white, font),
            (ClockHoursBall, navy, smallFont), (ClockMinutesBall, white, smallFont), (ClockSecondsBall, white, smallFont),
            (SleepsBall, white, (Sign?.SleepsNumber.Length ?? 0) > 2 ? bigFont * 0.75 : bigFont),
        })
        {
            digits.Look = new DigitLook(size, size * CellPerEm, size * 1.05, ink);
            digits.Animate = animate;
        }

        foreach (var text in new[] { DaysBallLabel, HoursBallLabel, MinutesBallLabel, SecondsBallLabel })
        {
            text.FontFamily = DisplayFont;
            text.FontSize = label;
        }

        SleepsBallWord.FontFamily = DisplayFont;
        SleepsBallWord.FontSize = compact ? 17 : 21;
        BallNextChange.FontFamily = DisplayFont;
        BallNextChange.FontSize = compact ? 14 : 17;
        MerryBallText.FontFamily = DisplayFont;
        MerryBallText.FontSize = compact ? 22 : 29;

        foreach (var bauble in new[] { DaysBauble, HoursBauble, MinutesBauble, SecondsBauble })
        {
            bauble.BallSize = ball;
        }

        SleepsBauble.BallSize = big;
        MerryBauble.BallSize = big;
        foreach (var bauble in new[] { ClockHoursBauble, ClockMinutesBauble, ClockSecondsBauble })
        {
            bauble.BallSize = small;
        }
    }

    private static FontFamily DisplayFont => (FontFamily)Application.Current.Resources["DisplayFontFamily"];

    private static Visibility Show(bool value) => value ? Visibility.Visible : Visibility.Collapsed;

    private static void Text(TextBlock block, FontFamily font, double size, Brush brush)
    {
        block.FontFamily = font;
        block.FontSize = size;
        block.Foreground = brush;
    }

    private void OnBoardSizeChanged(object sender, SizeChangedEventArgs e)
    {
        var (w, h) = (e.NewSize.Width, e.NewSize.Height);

        foreach (var path in new[] { SnowCap, BiscuitFill, IcingLine, TagPaper })
        {
            path.Width = w;
        }

        BiscuitFill.Height = h;
        IcingLine.Height = h;
        TagPaper.Height = h;

        BiscuitFill.Data = Scallops(15, 15, w - 30, h - 30, 26);
        IcingLine.Data = Scallops(23, 23, w - 46, h - 46, 13);
        TagPaper.Data = TagShape(w, h, 40);
    }

    /// <summary>A rectangle whose edges are little outward arcs, clockwise from the top left.</summary>
    private static PathGeometry Scallops(double x, double y, double width, double height, double step)
    {
        var across = Math.Max(2, (int)Math.Round(width / step));
        var down = Math.Max(2, (int)Math.Round(height / step));
        var (dx, dy) = (width / across, height / down);

        PathFigure figure = new() { StartPoint = new(x, y), IsClosed = true };
        void Arcs(int count, double stepX, double stepY)
        {
            var radius = Math.Max(Math.Abs(stepX), Math.Abs(stepY)) / 2;
            for (var i = 0; i < count; i++)
            {
                x += stepX;
                y += stepY;
                figure.Segments.Add(new ArcSegment { Point = new(x, y), Size = new(radius, radius), SweepDirection = SweepDirection.Clockwise });
            }
        }

        Arcs(across, dx, 0);
        Arcs(down, 0, dy);
        Arcs(across, -dx, 0);
        Arcs(down, 0, -dy);

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        return geometry;
    }

    private static PathGeometry TagShape(double width, double height, double chamfer)
    {
        PathFigure figure = new() { StartPoint = new(chamfer, 0), IsClosed = true };
        foreach (var point in new Windows.Foundation.Point[] { new(width - chamfer, 0), new(width, chamfer), new(width, height), new(0, height), new(0, chamfer) })
        {
            figure.Segments.Add(new LineSegment { Point = point });
        }

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        return geometry;
    }

    public static readonly DependencyProperty ShakeProperty = DependencyProperty.Register(
        nameof(Shake), typeof(ShakeEventArgs), typeof(CountdownSign), new PropertyMetadata(null, (d, e) => ((CountdownSign)d).OnShaken(e.NewValue)));

    private static readonly TimeSpan _swingLength = TimeSpan.FromMilliseconds(1300);
    private long _lastShakeSwing;

    /// <summary>The latest push of a shake. The sign swings as it does when tapped.</summary>
    public ShakeEventArgs? Shake
    {
        get => (ShakeEventArgs?)GetValue(ShakeProperty);
        set => SetValue(ShakeProperty, value);
    }

    // A shake is many pushes; restarting the swing on each would make it stutter.
    private void OnShaken(object? shake)
    {
        if (shake is not null && (_lastShakeSwing == 0 || Stopwatch.GetElapsedTime(_lastShakeSwing) >= _swingLength))
        {
            _lastShakeSwing = Stopwatch.GetTimestamp();
            Swing();
        }
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e) => Swing();

    private void Swing()
    {
        if (CalmMotion || IsPreview)
        {
            return;
        }

        if (Sign?.Design == SignDesign.Baubles)
        {
            for (var i = 0; i < _baubles.Length; i++)
            {
                PlaySwing(_baubles[i].Swing, TimeSpan.FromMilliseconds(i * 70));
            }
        }
        else
        {
            PlaySwing(SwingTransform, TimeSpan.Zero);
        }
    }

    private static void PlaySwing(RotateTransform transform, TimeSpan delay)
    {
        (double Ms, double Angle)[] frames = [(0, 0), (234, 2.6), (546, -1.9), (858, 1), (1105, -0.4), (1300, 0)];
        DoubleAnimationUsingKeyFrames animation = new() { BeginTime = delay };
        foreach (var (ms, angle) in frames)
        {
            animation.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(ms)),
                Value = angle,
                EasingFunction = new SineEase(),
            });
        }

        Storyboard.SetTarget(animation, transform);
        Storyboard.SetTargetProperty(animation, nameof(RotateTransform.Angle));
        Storyboard storyboard = new();
        storyboard.Children.Add(animation);
        storyboard.Begin();
    }

    private sealed record BoardSize(double PlateWidth, double PlateHeight, double PlateFont, double LabelSize, double GroupGap, double BigFont, double MerryFont)
    {
        public static readonly BoardSize Regular = new(44, 66, 50, 20, 12, 112, 56);
        public static readonly BoardSize Compact = new(32, 50, 37, 15, 6, 92, 34);
    }

    private sealed record SignLook(
        Thickness Padding,
        Brush Hanger,
        double HangerWidth,
        bool SingleString,
        Brush PlateInk,
        Brush PlateFill,
        double PlateCorner,
        Brush Label,
        Brush Big,
        bool UseHandwriting = false,
        Brush? Hinge = null,
        Brush? Perforation = null)
    {
        private static readonly SignLook Plank = new(
            new Thickness(16, 26, 16, 10), SignColors.Brush(0xC2A873), 7, false,
            SignColors.Brush(0x3B1E14), SignColors.Brush(0xF6E3BE), 6,
            SignColors.Brush(0x2B150B), SignColors.Brush(0x2B150B));

        private static readonly SignLook Gingerbread = new(
            new Thickness(32, 30, 32, 26), SignColors.Brush(0xC8102E), 11, false,
            SignColors.Brush(0xB51F3A), SignColors.Brush(0xFFF3DF), 9,
            SignColors.Brush(0xFFFFFF), SignColors.Brush(0xFFFFFF));

        private static readonly SignLook ParcelTag = new(
            new Thickness(20, 34, 20, 12), SignColors.Brush(0xE9E0D0), 4, true,
            SignColors.Brush(0xB51F3A), SignColors.Brush(0xFFFDF6), 0,
            SignColors.Brush(0x3A2412), SignColors.Brush(0xA61B34),
            UseHandwriting: true, Perforation: SignColors.Brush(0xD6B37E));

        private static readonly SignLook Station = new(
            new Thickness(22, 16, 22, 12), SignColors.Brush(0x2A2F3E), 6, false,
            SignColors.Brush(0xFFF9EA), SignColors.Brush(0x2F3B6B), 6,
            SignColors.Brush(0xFFE9B8), SignColors.Brush(0xFFF4D6),
            Hinge: SignColors.Brush(0x05070F));

        public static SignLook For(SignDesign design) => design switch
        {
            SignDesign.Gingerbread => Gingerbread,
            SignDesign.ParcelTag => ParcelTag,
            SignDesign.Station => Station,
            _ => Plank,
        };
    }
}
