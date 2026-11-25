using SkiaSharp;

namespace MerryCountdown.Scenes;

public sealed class ToyTrainScene : Scene
{
    private const float LapSeconds = 26f;
    private const float WhistleSeconds = 2f;
    private const float ShakeSettle = 2f;

    private float _trainTapped = float.NegativeInfinity;

    public override string Id => "train";

    protected override void PaintSky(SKCanvas canvas, int width, int height)
    {
        var unit = Math.Min(width, height);
        Painter.Sky(canvas, width, height, new SKColor(0x10, 0x10, 0x3A), new SKColor(0x37, 0x2B, 0x7A), new SKColor(0xB0, 0x5E, 0x8F));
        Painter.Stars(canvas, width, height, 0.55f, 27, 140);
        Painter.Moon(canvas, width * 0.16f, height * 0.16f, unit * 0.06f);
    }

    protected override void PaintForeground(SKCanvas canvas, int width, int height)
    {
        var unit = Math.Min(width, height);
        Painter.Hill(canvas, width, height, height * 0.6f, height * 0.06f, 1.1f, 1f, new SKColor(0xB7, 0x9B, 0xD8), new SKColor(0x7A, 0x6A, 0xB8));

        Random random = new(8);
        for (var x = 0f; x < width; x += unit * 0.09f * (0.5f + random.NextSingle()))
        {
            var ground = Painter.HillY(x, width, height * 0.6f, height * 0.06f, 1.1f, 1f) + (unit * 0.01f);
            Painter.Pine(canvas, x, ground, unit * (0.08f + (random.NextSingle() * 0.07f)), new SKColor(0x2B, 0x3F, 0x6B), new SKColor(0xE3, 0xDA, 0xF6));
        }

        Painter.Hill(canvas, width, height, height * 0.8f, 0, 1, 0, Palette.Snow, Palette.SnowShade);
        PaintTrack(canvas, width, height);

        Painter.Pine(canvas, width * 0.82f, height * 0.78f, unit * 0.3f, Palette.Pine, Palette.Snow);
        Painter.Pine(canvas, width * 0.3f, height * 0.77f, unit * 0.2f, Palette.PineDark, Palette.Snow);
        Painter.Hill(canvas, width, height, height * 0.93f, height * 0.015f, 2f, 2f, SKColors.White, Palette.Snow);
    }

    protected override void PaintSkyLife(SKCanvas canvas, int width, int height, float time) =>
        Painter.Twinkles(canvas, width, height, 0.45f, time, 6, 12);

    protected override void PaintForegroundLife(SKCanvas canvas, int width, int height, float time)
    {
        var (head, rail, carLength) = Train(width, height, time);
        var tapped = Remaining(_trainTapped, time, WhistleSeconds);
        var shaken = Shaken(time, ShakeSettle);
        var whistle = Math.Max(tapped, shaken);

        // A tap bounces the cars from the moment of the tap; a shake keeps them bouncing for as long as it lasts.
        var age = tapped >= shaken ? (1 - tapped) * WhistleSeconds : time;

        // The engine's lamp flashes either way. Only without Calm motion do the cars bounce and the steam billow.
        if (whistle > 0)
        {
            var lampX = head + (carLength * 0.85f);
            Painter.Glow(canvas, lampX, rail - (carLength * 0.4f), carLength * 1.4f, Palette.Candle, (byte)((CalmMotion ? 110 : 220) * whistle));
        }

        var bounce = CalmMotion ? 0 : whistle;

        SKColor[] cars = [Palette.Cranberry, new(0x2F, 0x6B, 0xC4), new(0x1F, 0x7A, 0x5A)];
        for (var i = cars.Length - 1; i >= 0; i--)
        {
            var hop = MathF.Abs(MathF.Sin((age * 8) - ((i + 1) * 0.8f))) * carLength * 0.14f * bounce;
            Carriage(canvas, head - ((i + 1) * carLength * 1.08f), rail - hop, carLength, cars[i], time, i);
        }

        Engine(canvas, head, rail - (MathF.Abs(MathF.Sin(age * 8)) * carLength * 0.14f * bounce), carLength * 1.15f, time, bounce);
    }

    protected override IEnumerable<SceneElement> ForegroundElements(int width, int height, float time)
    {
        var (head, rail, carLength) = Train(width, height, time);
        var left = head - (3 * carLength * 1.08f);
        var right = head + (carLength * 1.15f);
        yield return new SceneElement(new SKRect(left, rail - (carLength * 0.95f), right, rail), tappedAt => _trainTapped = tappedAt);
    }

    private static (float Head, float Rail, float CarLength) Train(int width, int height, float time)
    {
        var carLength = Math.Min(width, height) * 0.17f;
        var trainLength = carLength * 4.4f;
        var progress = (time % LapSeconds) / LapSeconds;
        return ((-trainLength * 0.4f) + (progress * (width + (trainLength * 1.4f))), height * 0.875f, carLength);
    }

    private static void PaintTrack(SKCanvas canvas, int width, int height)
    {
        var unit = Math.Min(width, height);
        var rail = height * 0.875f;
        using SKPaint paint = new() { IsAntialias = true, Color = Palette.WoodDark };
        for (var x = 0f; x < width; x += unit * 0.035f)
        {
            canvas.DrawRoundRect(x, rail - (unit * 0.002f), unit * 0.012f, unit * 0.022f, 2, 2, paint);
        }

        paint.Color = new SKColor(0x55, 0x5E, 0x7E);
        canvas.DrawRect(0, rail - (unit * 0.004f), width, unit * 0.007f, paint);
    }

    private static void Engine(SKCanvas canvas, float x, float rail, float length, float time, float billow)
    {
        var h = length * 0.56f;
        var wheel = length * 0.11f;
        var top = rail - wheel - h;
        using SKPaint paint = new() { IsAntialias = true, Color = Palette.Night };

        canvas.DrawRoundRect(x, top + (h * 0.35f), length * 0.72f, h * 0.65f, h * 0.12f, h * 0.12f, paint);
        paint.Color = Palette.Cranberry;
        canvas.DrawRoundRect(x, top - (h * 0.12f), length * 0.34f, h * 1.12f, h * 0.08f, h * 0.08f, paint);

        // Roofs go on after the windows, or the glow would stain them.
        Painter.Window(canvas, x + (length * 0.08f), top + (h * 0.03f), length * 0.17f, h * 0.34f, Palette.Candle);
        paint.Color = Palette.Night;
        canvas.DrawRoundRect(x - (length * 0.03f), top - (h * 0.2f), length * 0.4f, h * 0.12f, h * 0.05f, h * 0.05f, paint);

        paint.Color = Palette.Candle;
        canvas.DrawRect(x + (length * 0.42f), top + (h * 0.42f), length * 0.05f, h * 0.5f, paint);

        var noseX = x + (length * 0.72f);
        paint.Color = Palette.Candle;
        Painter.Glow(canvas, noseX + (length * 0.05f), top + (h * 0.65f), length * 0.3f, Palette.Candle, 180);
        canvas.DrawCircle(noseX + (length * 0.01f), top + (h * 0.65f), h * 0.14f, paint);

        paint.Color = Palette.Night;
        var stackX = x + (length * 0.58f);
        canvas.DrawRect(stackX, top + (h * 0.04f), length * 0.07f, h * 0.34f, paint);
        canvas.DrawRoundRect(stackX - (length * 0.025f), top - (h * 0.02f), length * 0.12f, h * 0.08f, h * 0.03f, h * 0.03f, paint);

        Puffs(canvas, stackX + (length * 0.035f), top - (h * 0.02f), length, time, billow);
        Wheels(canvas, x + (length * 0.15f), rail, wheel, length * 0.42f, 2, time);
    }

    private static void Carriage(SKCanvas canvas, float x, float rail, float length, SKColor color, float time, int index)
    {
        var wheel = length * 0.1f;
        var height = length * 0.56f;
        var top = rail - wheel - height;
        using SKPaint paint = new() { IsAntialias = true, Color = color };
        canvas.DrawRoundRect(x, top, length, height, height * 0.1f, height * 0.1f, paint);

        const int Windows = 3;
        var windowWidth = length * 0.2f;
        for (var i = 0; i < Windows; i++)
        {
            var phase = 0.92f + (0.08f * MathF.Sin((time * 2) + index + i));
            var light = new SKColor(Palette.Candle.Red, (byte)(Palette.Candle.Green * phase), Palette.Candle.Blue);
            Painter.Window(canvas, x + (length * 0.09f) + (i * length * 0.29f), top + (height * 0.2f), windowWidth, height * 0.42f, light);
        }

        paint.Color = Palette.Snow;
        canvas.DrawRoundRect(x - (length * 0.02f), top - (height * 0.09f), length * 1.04f, height * 0.16f, height * 0.07f, height * 0.07f, paint);

        paint.Color = Palette.Night;
        canvas.DrawRect(x + length, rail - (wheel * 1.5f), length * 0.09f, wheel * 0.36f, paint);
        Wheels(canvas, x + (length * 0.22f), rail, wheel, length * 0.56f, 2, time);
    }

    private static void Wheels(SKCanvas canvas, float x, float rail, float radius, float spacing, int count, float time)
    {
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < count; i++)
        {
            var cx = x + (i * spacing);
            var cy = rail - radius;
            paint.Color = Palette.Night;
            canvas.DrawCircle(cx, cy, radius, paint);
            paint.Color = Palette.Candle;
            canvas.DrawCircle(cx, cy, radius * 0.42f, paint);
            paint.Color = Palette.Night;
            var angle = time * 5;
            canvas.DrawCircle(cx + (MathF.Cos(angle) * radius * 0.25f), cy + (MathF.Sin(angle) * radius * 0.25f), radius * 0.13f, paint);
        }
    }

    private static void Puffs(SKCanvas canvas, float x, float y, float length, float time, float billow)
    {
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < 6; i++)
        {
            var progress = ((time * 0.45f) + (i / 6f)) % 1f;
            var size = length * (0.04f + (progress * 0.12f)) * (1 + (billow * 1.5f));
            paint.Color = SKColors.White.WithAlpha((byte)(190 * (1 - progress)));
            canvas.DrawCircle(x - (progress * length * 0.7f), y - (progress * length * 0.6f) + (MathF.Sin(progress * 9) * length * 0.02f), size, paint);
        }
    }
}
