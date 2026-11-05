using SkiaSharp;

namespace MerryCountdown.Scenes;

/// <summary>Tap to shake: the snow inside whirls up, then slowly settles again.</summary>
public sealed class SnowGlobeScene : Scene
{
    private const int Flakes = 180;
    private const float ShakeDecay = 0.45f;

    private float _lastShake = -100;

    public override string Id => "globe";

    public override void Tap(float x, float y, float time)
    {
        _lastShake = time;
        base.Tap(x, y, time);
    }

    protected override void PaintBackdrop(SKCanvas canvas, int width, int height)
    {
        Painter.Sky(canvas, width, height, new SKColor(0x16, 0x10, 0x2E), new SKColor(0x3A, 0x14, 0x3C), new SKColor(0x6A, 0x1C, 0x3C));
        Painter.Stars(canvas, width, height, 0.7f, 21, 90);

        var (cx, cy, radius) = Geometry(width, height);
        Painter.Glow(canvas, cx, cy, radius * 1.9f, Palette.Candle, 75);

        using SKPaint table = new() { IsAntialias = true };
        using var tableShader = SKShader.CreateLinearGradient(new SKPoint(0, height * 0.86f), new SKPoint(0, height), [Palette.Wood, Palette.WoodDark], SKShaderTileMode.Clamp);
        table.Shader = tableShader;
        canvas.DrawRect(0, cy + (radius * 1.2f), width, height, table);

        PaintWorld(canvas, cx, cy, radius);
        PaintBase(canvas, cx, cy, radius);
    }

    protected override void PaintLife(SKCanvas canvas, int width, int height, float time)
    {
        var (cx, cy, radius) = Geometry(width, height);
        Painter.Twinkles(canvas, width, height, 0.4f, time, 3, 10);
        PaintGlobeSnow(canvas, cx, cy, radius, time);
        PaintGlass(canvas, cx, cy, radius);
    }

    // The globe has its own snow, and a blizzard around a glass ball would look like a bug.
    protected override void PaintSnow(SKCanvas canvas, int width, int height, float time)
    {
    }

    private static (float X, float Y, float Radius) Geometry(int width, int height)
    {
        var radius = Math.Min(width * 0.36f, height * 0.27f);
        return (width / 2f, height - (radius * 1.62f), radius);
    }

    private static void PaintWorld(SKCanvas canvas, float cx, float cy, float radius)
    {
        canvas.Save();
        using SKPath clip = new();
        clip.AddCircle(cx, cy, radius);
        canvas.ClipPath(clip, antialias: true);

        using var skyShader = SKShader.CreateLinearGradient(new SKPoint(0, cy - radius), new SKPoint(0, cy + radius), [Palette.Dusk, new SKColor(0x5C, 0x6F, 0xB8)], SKShaderTileMode.Clamp);
        using SKPaint sky = new() { Shader = skyShader };
        canvas.DrawRect(cx - radius, cy - radius, radius * 2, radius * 2, sky);

        var ground = cy + (radius * 0.5f);
        using SKPaint snow = new() { IsAntialias = true, Color = Palette.Snow };
        canvas.DrawOval(cx, ground + (radius * 0.45f), radius * 1.15f, radius * 0.6f, snow);

        Painter.Pine(canvas, cx - (radius * 0.48f), ground, radius * 0.75f, Palette.Pine, Palette.Snow);
        Painter.Pine(canvas, cx + (radius * 0.62f), ground + (radius * 0.04f), radius * 0.48f, Palette.PineDark, Palette.Snow);
        Painter.House(canvas, cx + (radius * 0.1f), ground + (radius * 0.04f), radius * 0.56f, radius * 0.38f, Palette.Cranberry, Palette.Snow);
        Painter.Window(canvas, cx + (radius * 0.0f), ground - (radius * 0.2f), radius * 0.13f, radius * 0.15f, Palette.Candle);

        PaintSnowman(canvas, cx - (radius * 0.08f), ground + (radius * 0.3f), radius * 0.2f);
        canvas.Restore();
    }

    private static void PaintSnowman(SKCanvas canvas, float x, float y, float size)
    {
        using SKPaint paint = new() { IsAntialias = true, Color = Palette.SnowShade };
        canvas.DrawCircle(x + (size * 0.04f), y, size * 0.52f, paint);
        paint.Color = SKColors.White;
        canvas.DrawCircle(x, y - (size * 0.02f), size * 0.5f, paint);
        canvas.DrawCircle(x, y - (size * 0.68f), size * 0.34f, paint);
        paint.Color = Palette.Ember;
        canvas.DrawOval(x + (size * 0.14f), y - (size * 0.68f), size * 0.13f, size * 0.04f, paint);
        paint.Color = Palette.WoodDark;
        canvas.DrawCircle(x - (size * 0.1f), y - (size * 0.77f), size * 0.035f, paint);
        canvas.DrawCircle(x + (size * 0.1f), y - (size * 0.77f), size * 0.035f, paint);
        paint.Color = Palette.Cranberry;
        canvas.DrawRoundRect(x - (size * 0.3f), y - (size * 0.46f), size * 0.6f, size * 0.1f, size * 0.05f, size * 0.05f, paint);
    }

    private static void PaintBase(SKCanvas canvas, float cx, float cy, float radius)
    {
        var top = cy + (radius * 0.82f);
        var bottom = cy + (radius * 1.34f);
        using SKPath path = new();
        path.MoveTo(cx - (radius * 0.72f), top);
        path.LineTo(cx + (radius * 0.72f), top);
        path.QuadTo(cx + (radius * 0.86f), bottom - (radius * 0.2f), cx + (radius * 0.92f), bottom);
        path.LineTo(cx - (radius * 0.92f), bottom);
        path.QuadTo(cx - (radius * 0.86f), bottom - (radius * 0.2f), cx - (radius * 0.72f), top);
        path.Close();
        using var shader = SKShader.CreateLinearGradient(new SKPoint(cx - radius, 0), new SKPoint(cx + radius, 0), [Palette.Berry, Palette.Cranberry, Palette.Berry], SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader, IsAntialias = true };
        canvas.DrawPath(path, paint);

        using SKPaint gold = new() { IsAntialias = true, Color = Palette.Candle };
        canvas.DrawRect(cx - (radius * 0.84f), bottom - (radius * 0.2f), radius * 1.68f, radius * 0.045f, gold);
        for (var i = -2; i <= 2; i++)
        {
            Painter.Star(canvas, gold, cx + (i * radius * 0.28f), top + (radius * 0.2f), radius * 0.05f, 5, 0.45f, 0);
        }
    }

    private void PaintGlobeSnow(SKCanvas canvas, float cx, float cy, float radius, float time)
    {
        var shake = MathF.Exp(-Math.Max(0, time - _lastShake) * ShakeDecay);

        canvas.Save();
        using SKPath clip = new();
        clip.AddCircle(cx, cy, radius);
        canvas.ClipPath(clip, antialias: true);

        Random random = new(77);
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < Flakes; i++)
        {
            var seedX = (random.NextSingle() * 2) - 1;
            var offset = random.NextSingle();
            var speed = 0.04f + (random.NextSingle() * 0.06f);
            var phase = random.NextSingle() * MathF.Tau;
            var size = radius * (0.006f + (random.NextSingle() * 0.012f));

            var progress = ((time * speed) + offset) % 1f;
            var calmX = cx + (seedX * radius * 0.92f) + (MathF.Sin((time * 0.9f) + phase) * radius * 0.03f);
            var calmY = cy - radius + (progress * radius * 2);

            var swirl = (time * (1.2f + speed * 8)) + phase;
            var ring = radius * (0.2f + (0.72f * MathF.Abs(seedX)));
            var wildX = cx + (MathF.Cos(swirl) * ring);
            var wildY = cy + (MathF.Sin(swirl) * ring * 0.9f);

            paint.Color = SKColors.White.WithAlpha((byte)(170 + (85 * offset)));
            canvas.DrawCircle(calmX + ((wildX - calmX) * shake), calmY + ((wildY - calmY) * shake), size, paint);
        }

        canvas.Restore();
    }

    private static void PaintGlass(SKCanvas canvas, float cx, float cy, float radius)
    {
        using var rim = SKShader.CreateRadialGradient(new SKPoint(cx, cy), radius, [SKColors.Transparent, SKColors.White.WithAlpha(90)], [0.82f, 1f], SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = rim, IsAntialias = true };
        canvas.DrawCircle(cx, cy, radius, paint);

        using SKPaint shine = new() { IsAntialias = true, Color = SKColors.White.WithAlpha(70) };
        canvas.Save();
        canvas.RotateDegrees(-35, cx - (radius * 0.45f), cy - (radius * 0.5f));
        canvas.DrawOval(cx - (radius * 0.45f), cy - (radius * 0.5f), radius * 0.34f, radius * 0.13f, shine);
        canvas.Restore();
    }
}
