using SkiaSharp;

namespace MerryCountdown.Scenes;

public sealed class VillageScene : Scene
{
    public override string Id => "village";

    protected override void PaintBackdrop(SKCanvas canvas, int width, int height)
    {
        var unit = Math.Min(width, height);
        Painter.Sky(canvas, width, height, Palette.Night, Palette.Dusk, Palette.Haze);
        Painter.Stars(canvas, width, height, 0.65f, 7, 170);
        Painter.Moon(canvas, width * 0.82f, height * 0.17f, unit * 0.075f);

        Painter.Hill(canvas, width, height, height * 0.62f, height * 0.05f, 1.4f, 0.6f, Palette.SnowDeep, Palette.Haze);
        ForestLine(canvas, width, height, height * 0.62f, 1.4f, 0.6f, height * 0.05f);

        var ground = height * 0.74f;
        Painter.Hill(canvas, width, height, ground, height * 0.02f, 1f, 2.2f, Palette.SnowShade, Palette.SnowDeep);
        PaintVillage(canvas, width, height, ground);

        Painter.Hill(canvas, width, height, height * 0.87f, height * 0.025f, 1.6f, 4f, Palette.Snow, Palette.SnowShade);
        Painter.Pine(canvas, width * 0.06f, height * 0.9f, unit * 0.34f, Palette.PineDark, Palette.Snow);
        Painter.Pine(canvas, width * 0.95f, height * 0.91f, unit * 0.28f, Palette.PineDark, Palette.Snow);
    }

    protected override void PaintLife(SKCanvas canvas, int width, int height, float time)
    {
        Painter.Twinkles(canvas, width, height, 0.55f, time, 19, 14);
        PaintTreeLights(canvas, width, height, time);
    }

    private static void ForestLine(SKCanvas canvas, int width, int height, float baseline, float waves, float phase, float swell)
    {
        Random random = new(42);
        var unit = Math.Min(width, height);
        var spacing = unit * 0.05f;
        for (var x = -spacing; x < width + spacing; x += spacing * (0.6f + random.NextSingle()))
        {
            var tree = unit * (0.07f + (random.NextSingle() * 0.05f));
            var ground = Painter.HillY(x, width, baseline, swell, waves, phase) + (unit * 0.012f);
            Painter.Pine(canvas, x, ground, tree, Palette.Pine.WithAlpha(235), Palette.SnowShade);
        }
    }

    private static void PaintVillage(SKCanvas canvas, int width, int height, float ground)
    {
        var unit = Math.Min(width, height);
        var houseCount = Math.Max(4, (int)(width / (unit * 0.2f)));
        SKColor[] walls = [new(0x8C, 0x3B, 0x3B), new(0x3E, 0x5C, 0x7E), new(0xA2, 0x6B, 0x3F), new(0x4B, 0x6B, 0x57), new(0x6F, 0x46, 0x7D)];
        Random random = new(5);

        for (var i = 0; i < houseCount; i++)
        {
            var x = (width * (i + 0.5f) / houseCount) + ((random.NextSingle() - 0.5f) * unit * 0.03f);
            var houseWidth = unit * (0.13f + (random.NextSingle() * 0.05f));
            var houseHeight = houseWidth * (0.62f + (random.NextSingle() * 0.2f));
            var groundHere = Painter.HillY(x, width, ground, height * 0.02f, 1f, 2.2f) + (unit * 0.012f);

            Painter.House(canvas, x, groundHere, houseWidth, houseHeight, walls[i % walls.Length], Palette.Snow);

            var windows = random.Next(1, 3);
            var windowWidth = houseWidth * 0.2f;
            for (var w = 0; w < windows; w++)
            {
                var wx = x - (windows * windowWidth * 0.75f) + (w * windowWidth * 1.5f) + (windowWidth * 0.25f);
                Painter.Window(canvas, wx, groundHere - (houseHeight * 0.72f), windowWidth, windowWidth * 1.15f, Palette.Candle);
            }
        }
    }

    private static void PaintTreeLights(SKCanvas canvas, int width, int height, float time)
    {
        // The town tree stands in the front so the sky stays clear for the big number.
        var unit = Math.Min(width, height);
        var x = width * 0.5f;
        var ground = height * 0.94f;
        var treeHeight = unit * 0.34f;

        Painter.Glow(canvas, x, ground - (treeHeight * 0.5f), treeHeight * 0.85f, Palette.Candle, 40);
        Painter.Pine(canvas, x, ground, treeHeight, Palette.Pine, Palette.Snow);

        Random random = new(99);
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < 26; i++)
        {
            var along = random.NextSingle();
            var side = (random.NextSingle() * 2) - 1;
            var ly = ground - (treeHeight * 0.92f) + (along * treeHeight * 0.74f);
            var lx = x + (side * along * treeHeight * 0.24f);
            var color = Palette.Lights[i % Palette.Lights.Length];
            var pulse = 0.55f + (0.45f * MathF.Sin((time * 2.1f) + (i * 1.7f)));
            var radius = unit * 0.0075f;
            Painter.Glow(canvas, lx, ly, radius * 3.4f, color, (byte)(120 * pulse));
            paint.Color = color.WithAlpha((byte)(150 + (105 * pulse)));
            canvas.DrawCircle(lx, ly, radius, paint);
        }

        paint.Color = Palette.Candle;
        var shine = 1 + (0.08f * MathF.Sin(time * 1.5f));
        Painter.Glow(canvas, x, ground - treeHeight, unit * 0.07f * shine, Palette.Candle, 170);
        Painter.Star(canvas, paint, x, ground - treeHeight, unit * 0.032f * shine, 5, 0.45f, 0);
    }
}
