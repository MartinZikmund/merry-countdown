using SkiaSharp;

namespace MerryCountdown.Scenes;

public sealed class AuroraScene : Scene
{
    public override string Id => "aurora";

    protected override void PaintSky(SKCanvas canvas, int width, int height)
    {
        Painter.Sky(canvas, width, height, new SKColor(0x05, 0x0B, 0x24), new SKColor(0x0B, 0x1E, 0x46), new SKColor(0x16, 0x3B, 0x5C));
        Painter.Stars(canvas, width, height, 0.7f, 11, 230);
    }

    protected override void PaintSkyLife(SKCanvas canvas, int width, int height, float time)
    {
        Painter.Twinkles(canvas, width, height, 0.5f, time, 8, 12);
        Ribbon(canvas, width, height, time, 0.24f, 0.17f, 0.22f, Palette.Mint, 0f);
        Ribbon(canvas, width, height, time, 0.32f, 0.13f, 0.17f, new SKColor(0x7A, 0x6B, 0xFF), 2.1f);
        Ribbon(canvas, width, height, time, 0.17f, 0.1f, 0.12f, new SKColor(0xFF, 0x7E, 0xC8), 4.2f);
    }

    protected override void PaintForegroundLife(SKCanvas canvas, int width, int height, float time) =>
        PaintLand(canvas, width, height, time);

    private static void Ribbon(SKCanvas canvas, int width, int height, float time, float centre, float swing, float tall, SKColor color, float phase)
    {
        const int Step = 12;
        using SKPaint paint = new() { IsAntialias = false, BlendMode = SKBlendMode.Plus };
        for (var x = 0; x < width; x += Step)
        {
            var t = x / (float)width;
            var wave = MathF.Sin((t * 5f) + (time * 0.18f) + phase) + (0.5f * MathF.Sin((t * 11f) - (time * 0.13f) + (phase * 1.7f)));
            var y = height * (centre + (wave * swing * 0.5f));
            var column = height * tall * (0.75f + (0.25f * MathF.Sin((t * 23f) + (time * 0.4f) + phase)));
            var strength = 0.6f + (0.4f * MathF.Sin((t * 7f) + (time * 0.25f) + phase));
            using var shader = SKShader.CreateLinearGradient(new SKPoint(0, y - column), new SKPoint(0, y + (column * 0.25f)), [color.WithAlpha(0), color.WithAlpha((byte)(115 * strength)), color.WithAlpha(0)], [0f, 0.75f, 1f], SKShaderTileMode.Clamp);
            paint.Shader = shader;
            canvas.DrawRect(x, y - column, Step, column * 1.25f, paint);
        }

        paint.Shader = null;
    }

    private static void PaintLand(SKCanvas canvas, int width, int height, float time)
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

        var cabinX = width * 0.5f;
        var cabinGround = height * 0.875f;
        var cabin = unit * 0.3f;
        Painter.House(canvas, cabinX, cabinGround, cabin, cabin * 0.55f, Palette.WoodDark, Palette.Snow);
        var flicker = 0.92f + (0.08f * MathF.Sin(time * 5.3f));
        var light = new SKColor(Palette.Candle.Red, (byte)(Palette.Candle.Green * flicker), Palette.Candle.Blue);
        Painter.Window(canvas, cabinX - (cabin * 0.28f), cabinGround - (cabin * 0.38f), cabin * 0.17f, cabin * 0.2f, light);
        Painter.Window(canvas, cabinX + (cabin * 0.11f), cabinGround - (cabin * 0.38f), cabin * 0.17f, cabin * 0.2f, light);

        Painter.Pine(canvas, width * 0.13f, height * 0.93f, unit * 0.36f, Palette.Pine, Palette.Snow);
        Painter.Pine(canvas, width * 0.88f, height * 0.94f, unit * 0.31f, Palette.Pine, Palette.Snow);
    }
}
