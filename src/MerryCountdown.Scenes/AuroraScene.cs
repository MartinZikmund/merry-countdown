using SkiaSharp;

namespace MerryCountdown.Scenes;

public sealed class AuroraScene : Scene
{
    private const float LightsReaction = 4f;
    private const float CabinReaction = 2f;
    private const float ShakeSettle = 3f;

    private float _lightsTapped = float.NegativeInfinity;
    private float _cabinTapped = float.NegativeInfinity;
    private SKPoint[] _ribbonPoints = [];
    private SKColor[] _ribbonColors = [];

    public override string Id => "aurora";

    protected override void PaintSky(SKCanvas canvas, int width, int height)
    {
        Painter.Sky(canvas, width, height, new SKColor(0x05, 0x0B, 0x24), new SKColor(0x0B, 0x1E, 0x46), new SKColor(0x16, 0x3B, 0x5C));
        Painter.Stars(canvas, width, height, 0.7f, 11, 230);
    }

    protected override void PaintSkyLife(SKCanvas canvas, int width, int height, float time)
    {
        Painter.Twinkles(canvas, width, height, 0.5f, time, 8, 12);
        var flare = Math.Max(Remaining(_lightsTapped, time, LightsReaction), Shaken(time, ShakeSettle));
        var reach = CalmMotion ? 0 : flare;
        var glow = flare * (CalmMotion ? 0.35f : 1f);
        Ribbon(canvas, width, height, time, 0.24f, 0.17f, 0.22f, Palette.Mint, 0f, reach, glow);
        Ribbon(canvas, width, height, time, 0.32f, 0.13f, 0.17f, new SKColor(0x7A, 0x6B, 0xFF), 2.1f, reach, glow);
        Ribbon(canvas, width, height, time, 0.17f, 0.1f, 0.12f, new SKColor(0xFF, 0x7E, 0xC8), 4.2f, reach, glow);
    }

    // The land stands still and goes into the cached backdrop; only the cabin's window panes flicker.
    protected override void PaintForeground(SKCanvas canvas, int width, int height) =>
        PaintLand(canvas, width, height);

    protected override void PaintForegroundLife(SKCanvas canvas, int width, int height, float time) =>
        PaintCabinLights(canvas, width, height, time);

    protected override IEnumerable<SceneElement> ForegroundElements(int width, int height, float time)
    {
        var (x, ground, cabin) = Cabin(width, height);
        var halfRoof = cabin * 0.64f;
        yield return new SceneElement(new SKRect(x - halfRoof, ground - (cabin * 1.01f), x + halfRoof, ground), tappedAt => _cabinTapped = tappedAt);
    }

    protected override IEnumerable<SceneElement> SkyElements(int width, int height, float time)
    {
        yield return new SceneElement(new SKRect(0, height * 0.04f, width, height * 0.48f), tappedAt => _lightsTapped = tappedAt);
    }

    private static (float X, float Ground, float Size) Cabin(int width, int height) =>
        (width * 0.5f, height * 0.875f, Math.Min(width, height) * 0.3f);

    private static SKRect CabinWindow(int width, int height, int index)
    {
        var (x, ground, cabin) = Cabin(width, height);
        return SKRect.Create(x + (cabin * (index == 0 ? -0.28f : 0.11f)), ground - (cabin * 0.38f), cabin * 0.17f, cabin * 0.2f);
    }

    /// <summary>The lamp inside, from 0 at its dimmest to 2 at its brightest.</summary>
    private static SKColor CabinLight(float flicker) =>
        new(Palette.Candle.Red, (byte)(Palette.Candle.Green * (0.84f + (0.08f * flicker))), Palette.Candle.Blue);

    // Each ribbon is one mesh of 12-pixel columns, drawn in a single call: a draw per column was most of the frame on the GPU.
    private void Ribbon(SKCanvas canvas, int width, int height, float time, float centre, float swing, float tall, SKColor color, float phase, float reach, float glow)
    {
        const int Step = 12;
        const int PerColumn = 12;

        var columns = (width + Step - 1) / Step;
        if (_ribbonPoints.Length != columns * PerColumn)
        {
            _ribbonPoints = new SKPoint[columns * PerColumn];
            _ribbonColors = new SKColor[columns * PerColumn];
        }

        var clear = color.WithAlpha(0);
        for (var i = 0; i < columns; i++)
        {
            var x = i * Step;
            var t = x / (float)width;
            var wave = MathF.Sin((t * 5f) + (time * 0.18f) + phase) + (0.5f * MathF.Sin((t * 11f) - (time * 0.13f) + (phase * 1.7f)));
            var y = height * (centre + (wave * swing * 0.5f));
            var column = height * tall * (0.75f + (0.25f * MathF.Sin((t * 23f) + (time * 0.4f) + phase))) * (1 + (reach * 0.7f));
            var strength = (0.6f + (0.4f * MathF.Sin((t * 7f) + (time * 0.25f) + phase))) * (1 + (glow * 1.1f));
            var bright = color.WithAlpha((byte)Math.Min(255, 115 * strength));

            // Fades in over the top three quarters, out over the last quarter.
            var (top, peak, bottom) = (y - column, y - (column * 0.0625f), y + (column * 0.25f));
            var at = i * PerColumn;
            Quad(at, x, x + Step, top, peak, clear, bright);
            Quad(at + 6, x, x + Step, peak, bottom, bright, clear);
        }

        using var vertices = SKVertices.CreateCopy(SKVertexMode.Triangles, _ribbonPoints, _ribbonColors);
        using SKPaint paint = new() { IsAntialias = false, BlendMode = SKBlendMode.Plus, Color = SKColors.White };
        canvas.DrawVertices(vertices, SKBlendMode.Modulate, paint);
    }

    private void Quad(int at, float left, float right, float top, float bottom, SKColor upper, SKColor lower)
    {
        _ribbonPoints[at] = new SKPoint(left, top);
        _ribbonPoints[at + 1] = new SKPoint(right, top);
        _ribbonPoints[at + 2] = new SKPoint(right, bottom);
        _ribbonPoints[at + 3] = new SKPoint(left, top);
        _ribbonPoints[at + 4] = new SKPoint(right, bottom);
        _ribbonPoints[at + 5] = new SKPoint(left, bottom);
        _ribbonColors[at] = _ribbonColors[at + 1] = _ribbonColors[at + 3] = upper;
        _ribbonColors[at + 2] = _ribbonColors[at + 4] = _ribbonColors[at + 5] = lower;
    }

    private static void PaintLand(SKCanvas canvas, int width, int height)
    {
        var unit = Math.Min(width, height);
        Painter.Hill(canvas, width, height, height * 0.72f, height * 0.05f, 0.9f, 1.3f, new SKColor(0x5A, 0x7A, 0xA8), new SKColor(0x2A, 0x46, 0x72));

        Random random = new(14);
        for (var x = -20f; x < width + 20; x += unit * 0.06f * (0.5f + random.NextSingle()))
        {
            var ground = Painter.HillY(x, width, height * 0.72f, height * 0.05f, 0.9f, 1.3f) + (unit * 0.012f);
            Painter.Pine(canvas, x, ground, unit * (0.09f + (random.NextSingle() * 0.09f)), Palette.PineDark, new SKColor(0x9F, 0xB8, 0xDB));
        }

        Painter.Hill(canvas, width, height, height * 0.86f, height * 0.02f, 1.2f, 0.2f, Palette.SnowShade, Palette.SnowDeep);

        var (cabinX, cabinGround, cabin) = Cabin(width, height);
        Painter.House(canvas, cabinX, cabinGround, cabin, cabin * 0.55f, Palette.WoodDark, Palette.Snow, [CabinWindow(width, height, 0), CabinWindow(width, height, 1)], CabinLight(1));

        Painter.Pine(canvas, width * 0.13f, height * 0.93f, unit * 0.36f, Palette.Pine, Palette.Snow);
        Painter.Pine(canvas, width * 0.88f, height * 0.94f, unit * 0.31f, Palette.Pine, Palette.Snow);
    }

    private void PaintCabinLights(SKCanvas canvas, int width, int height, float time)
    {
        var (cabinX, cabinGround, cabin) = Cabin(width, height);
        var light = CabinLight(1 + MathF.Sin(time * 5.3f));
        for (var i = 0; i < 2; i++)
        {
            var window = CabinWindow(width, height, i);
            Painter.WindowPane(canvas, window.Left, window.Top, window.Width, window.Height, light);
        }

        // Someone inside turns the lamps up. A shake does it too, so there's still an answer under a photo.
        var lamps = Math.Max(Remaining(_cabinTapped, time, CabinReaction), Shaken(time, ShakeSettle));
        if (lamps > 0)
        {
            var alpha = (byte)((CalmMotion ? 110 : 235) * lamps);
            Painter.Glow(canvas, cabinX - (cabin * 0.195f), cabinGround - (cabin * 0.28f), cabin * 0.42f, Palette.Candle, alpha);
            Painter.Glow(canvas, cabinX + (cabin * 0.195f), cabinGround - (cabin * 0.28f), cabin * 0.42f, Palette.Candle, alpha);
        }
    }
}
