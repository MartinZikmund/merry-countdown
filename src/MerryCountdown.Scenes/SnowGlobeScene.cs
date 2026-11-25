using SkiaSharp;

namespace MerryCountdown.Scenes;

/// <summary>Tap the glass, or shake the device, and the snow inside whirls up, then slowly settles again.</summary>
public sealed class SnowGlobeScene : Scene
{
    private const int Flakes = 180;
    private const float ShakeDecay = 0.45f;
    private const float CalmShake = 0.4f;
    private const float SettleSeconds = 5f;

    // How far below the middle the socket's rim sits, and how big the rim and the hole in it are, in radii.
    private const float RimDrop = 0.8f;
    private const float RimWidth = 0.74f;
    private const float RimDepth = 0.1f;
    private const float SocketWidth = 0.69f;
    private const float SocketDepth = 0.075f;

    private float _lastShake = -100;
    private SKPath? _glass;
    private (float X, float Y, float Radius) _glassFor;

    public override string Id => "globe";

    protected override void PaintSky(SKCanvas canvas, int width, int height)
    {
        Painter.Sky(canvas, width, height, new SKColor(0x16, 0x10, 0x2E), new SKColor(0x3A, 0x14, 0x3C), new SKColor(0x6A, 0x1C, 0x3C));
        Painter.Stars(canvas, width, height, 0.7f, 21, 90);
    }

    protected override void PaintForeground(SKCanvas canvas, int width, int height)
    {
        var (cx, cy, radius) = Geometry(width, height);
        Painter.Glow(canvas, cx, cy, radius * 1.9f, Palette.Candle, 75);

        using SKPaint table = new() { IsAntialias = true };
        using var tableShader = SKShader.CreateLinearGradient(new SKPoint(0, height * 0.86f), new SKPoint(0, height), [Palette.Wood, Palette.WoodDark], SKShaderTileMode.Clamp);
        table.Shader = tableShader;
        canvas.DrawRect(0, cy + (radius * 1.2f), width, height, table);

        using SKPaint shadow = new() { IsAntialias = true, Color = SKColors.Black.WithAlpha(70) };
        canvas.DrawOval(cx, cy + (radius * 1.37f), radius * 1.02f, radius * 0.07f, shadow);

        // The glass goes on after the base, so it hides the back of the rim and the front of the rim hides it.
        PaintBase(canvas, cx, cy, radius);
        PaintWorld(canvas, Glass(cx, cy, radius), cx, cy, radius);
    }

    protected override void PaintSkyLife(SKCanvas canvas, int width, int height, float time) =>
        Painter.Twinkles(canvas, width, height, 0.4f, time, 3, 10);

    protected override void PaintForegroundLife(SKCanvas canvas, int width, int height, float time)
    {
        var (cx, cy, radius) = Geometry(width, height);
        var glass = Glass(cx, cy, radius);
        canvas.Save();
        canvas.ClipPath(glass, antialias: true);
        PaintGlobeSnow(canvas, cx, cy, radius, time);
        canvas.Restore();
        PaintGlass(canvas, glass, cx, cy, radius);
    }

    protected override IEnumerable<SceneElement> ForegroundElements(int width, int height, float time)
    {
        var (cx, cy, radius) = Geometry(width, height);
        yield return new SceneElement(new SKRect(cx - radius, cy - radius, cx + radius, cy + radius), tappedAt => _lastShake = tappedAt, IsRound: true);
    }

    // The globe has its own snow, and a blizzard around a glass ball would look like a bug.
    protected override void PaintSnow(SKCanvas canvas, int width, int height, float time)
    {
    }

    private (float X, float Y, float Radius) Geometry(int width, int height)
    {
        // Glass and base together stand 2.62 radii tall. The rest is air between the globe and the sign.
        var radius = Math.Min(Math.Min(width * 0.36f, height * 0.27f), height * ClearPart / 2.8f);
        return (width / 2f, height - (radius * 1.62f), radius);
    }

    protected override void Release() => _glass?.Dispose();

    /// <summary>The part of the ball that shows: all of it down to the rim, and below that only what sits in the socket.</summary>
    private SKPath Glass(float cx, float cy, float radius)
    {
        if (_glass is not null && _glassFor == (cx, cy, radius))
        {
            return _glass;
        }

        var rim = cy + (radius * RimDrop);
        using SKPath ball = new();
        ball.AddCircle(cx, cy, radius);
        using SKPath open = new();
        open.AddRect(new SKRect(cx - radius, cy - radius, cx + radius, rim));
        open.AddOval(new SKRect(cx - (radius * SocketWidth), rim - (radius * SocketDepth), cx + (radius * SocketWidth), rim + (radius * SocketDepth)));

        _glass?.Dispose();
        _glass = ball.Op(open, SKPathOp.Intersect);
        _glassFor = (cx, cy, radius);
        return _glass;
    }

    private static void PaintWorld(SKCanvas canvas, SKPath glass, float cx, float cy, float radius)
    {
        canvas.Save();
        canvas.ClipPath(glass, antialias: true);

        using var skyShader = SKShader.CreateLinearGradient(new SKPoint(0, cy - radius), new SKPoint(0, cy + radius), [Palette.Dusk, new SKColor(0x5C, 0x6F, 0xB8)], SKShaderTileMode.Clamp);
        using SKPaint sky = new() { Shader = skyShader };
        canvas.DrawRect(cx - radius, cy - radius, radius * 2, radius * 2, sky);

        var ground = cy + (radius * 0.5f);
        using SKPaint snow = new() { IsAntialias = true, Color = Palette.Snow };
        canvas.DrawOval(cx, ground + (radius * 0.45f), radius * 1.15f, radius * 0.6f, snow);

        Painter.Pine(canvas, cx - (radius * 0.48f), ground, radius * 0.75f, Palette.Pine, Palette.Snow);
        Painter.Pine(canvas, cx + (radius * 0.62f), ground + (radius * 0.04f), radius * 0.48f, Palette.PineDark, Palette.Snow);
        var window = SKRect.Create(cx, ground - (radius * 0.2f), radius * 0.13f, radius * 0.15f);
        Painter.House(canvas, cx + (radius * 0.1f), ground + (radius * 0.04f), radius * 0.56f, radius * 0.38f, Palette.Cranberry, Palette.Snow, [window], Palette.Candle);

        PaintSnowman(canvas, cx - (radius * 0.14f), ground + (radius * 0.24f), radius * 0.2f);
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
        var top = cy + (radius * RimDrop);
        var bottom = cy + (radius * 1.34f);

        // Seen from a little above, like the rim: the foot, the band and the row of stars all sag in the middle.
        var sag = radius * 0.05f;
        using SKPath path = new();
        path.MoveTo(cx - (radius * RimWidth), top);
        path.LineTo(cx + (radius * RimWidth), top);
        path.QuadTo(cx + (radius * 0.86f), bottom - (radius * 0.2f), cx + (radius * 0.92f), bottom);
        path.QuadTo(cx, bottom + (sag * 2), cx - (radius * 0.92f), bottom);
        path.QuadTo(cx - (radius * 0.86f), bottom - (radius * 0.2f), cx - (radius * RimWidth), top);
        path.Close();
        using var shader = SKShader.CreateLinearGradient(new SKPoint(cx - radius, 0), new SKPoint(cx + radius, 0), [Palette.Berry, Palette.Cranberry, Palette.Berry], SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader, IsAntialias = true };
        canvas.DrawPath(path, paint);

        using SKPaint gold = new() { IsAntialias = true, Color = Palette.Candle };
        canvas.Save();
        canvas.ClipPath(path, antialias: true);
        using SKPath band = new();
        var bandY = bottom - (radius * 0.18f);
        band.MoveTo(cx - radius, bandY);
        band.QuadTo(cx, bandY + (sag * 2), cx + radius, bandY);
        gold.Style = SKPaintStyle.Stroke;
        gold.StrokeWidth = radius * 0.045f;
        canvas.DrawPath(band, gold);
        gold.Style = SKPaintStyle.Fill;
        canvas.Restore();

        for (var i = -2; i <= 2; i++)
        {
            var across = i * 0.28f;
            Painter.Star(canvas, gold, cx + (across * radius), top + (radius * 0.23f) + (sag * (1 - (across * across))), radius * 0.05f, 5, 0.45f, 0);
        }

        // The rim the ball rests in, and the dark of the socket behind it.
        canvas.DrawOval(cx, top, radius * RimWidth, radius * RimDepth, gold);
        using SKPaint socket = new() { IsAntialias = true, Color = new SKColor(0x4A, 0x0C, 0x1E) };
        canvas.DrawOval(cx, top, radius * SocketWidth, radius * SocketDepth, socket);
    }

    private void PaintGlobeSnow(SKCanvas canvas, float cx, float cy, float radius, float time)
    {
        var tapped = MathF.Exp(-Math.Max(0, time - _lastShake) * ShakeDecay);
        var shake = Math.Max(tapped, Shaken(time, SettleSeconds)) * (CalmMotion ? CalmShake : 1);

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
    }

    private static void PaintGlass(SKCanvas canvas, SKPath glass, float cx, float cy, float radius)
    {
        // Filling the glass's own outline keeps the shine off the base without the cost of painting through a clip.
        using var rim = SKShader.CreateRadialGradient(new SKPoint(cx, cy), radius, [SKColors.Transparent, SKColors.White.WithAlpha(90)], [0.82f, 1f], SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = rim, IsAntialias = true };
        canvas.DrawPath(glass, paint);

        using SKPaint shine = new() { IsAntialias = true, Color = SKColors.White.WithAlpha(70) };
        canvas.Save();
        canvas.RotateDegrees(-35, cx - (radius * 0.45f), cy - (radius * 0.5f));
        canvas.DrawOval(cx - (radius * 0.45f), cy - (radius * 0.5f), radius * 0.34f, radius * 0.13f, shine);
        canvas.Restore();
    }
}
