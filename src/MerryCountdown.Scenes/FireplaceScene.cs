using SkiaSharp;

namespace MerryCountdown.Scenes;

public sealed class FireplaceScene : Scene
{
    private const float FlamesReaction = 2.5f;
    private const float StockingReaction = 1.6f;
    private const float PresentReaction = 1.1f;
    private const float StockingSwingDegrees = 16;
    private const float ShakeSettle = 2f;

    private readonly float[] _stockingTapped = [float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity];
    private readonly float[] _presentTapped = [float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity];
    private float _flamesTapped = float.NegativeInfinity;

    public override string Id => "fireplace";

    protected override void PaintSky(SKCanvas canvas, int width, int height)
    {
        Painter.Sky(canvas, width, height, new SKColor(0x2A, 0x0F, 0x24), new SKColor(0x4A, 0x18, 0x2C), new SKColor(0x6E, 0x26, 0x2C));
        PaintWallpaper(canvas, width, height);
    }

    protected override void PaintForeground(SKCanvas canvas, int width, int height)
    {
        var (cx, floor, hearth) = Geometry(width, height);
        using SKPaint paint = new() { IsAntialias = true };

        using var floorShader = SKShader.CreateLinearGradient(new SKPoint(0, floor), new SKPoint(0, height), [Palette.Wood, Palette.WoodDark], SKShaderTileMode.Clamp);
        paint.Shader = floorShader;
        canvas.DrawRect(0, floor, width, height - floor, paint);
        paint.Shader = null;

        var outerWidth = hearth * 1.5f;
        var outerHeight = hearth * 1.02f;
        paint.Color = new SKColor(0xB4, 0x57, 0x3E);
        using var body = TopRounded(new SKRect(cx - (outerWidth / 2), floor - outerHeight, cx + (outerWidth / 2), floor), hearth * 0.02f);
        canvas.DrawRoundRect(body, paint);
        canvas.Save();
        canvas.ClipRoundRect(body, antialias: true);
        PaintBricks(canvas, cx - (outerWidth / 2), floor - outerHeight, outerWidth, outerHeight, hearth);
        canvas.Restore();

        paint.Color = new SKColor(0x1A, 0x0C, 0x10);
        var opening = hearth * 0.92f;
        using var firebox = TopRounded(new SKRect(cx - (opening / 2), floor - (opening * 0.7f), cx + (opening / 2), floor), opening * 0.2f);
        canvas.DrawRoundRect(firebox, paint);

        paint.Color = Palette.Wood;
        canvas.DrawRoundRect(cx - (hearth * 0.86f), floor - outerHeight - (hearth * 0.07f), hearth * 1.72f, hearth * 0.09f, hearth * 0.015f, hearth * 0.015f, paint);

        PaintBackLog(canvas, cx, floor, opening);
        PaintGarland(canvas, cx, floor - outerHeight - (hearth * 0.07f), hearth);
    }

    protected override void PaintForegroundLife(SKCanvas canvas, int width, int height, float time)
    {
        var (cx, floor, hearth) = Geometry(width, height);
        var opening = hearth * 0.92f;
        var flicker = 0.85f + (0.15f * MathF.Sin(time * 7.3f) * MathF.Sin(time * 3.1f));

        // These two can move when tapped, so unlike the rest of the room they're painted fresh every frame.
        PaintStockings(canvas, width, height, time);
        PaintPresents(canvas, width, height, time);

        var shaken = Shaken(time, ShakeSettle);
        var roar = Math.Max(Remaining(_flamesTapped, time, FlamesReaction), shaken * 0.6f);
        var glow = (byte)(95 + (roar * (CalmMotion ? 45 : 90)));
        Painter.Glow(canvas, cx, floor - (opening * 0.2f), hearth * 1.5f * flicker, Palette.Ember, glow);
        PaintFlames(canvas, cx, floor - (opening * 0.11f), opening, time, CalmMotion ? 0 : roar);

        // The fire burns between the two logs, so this one goes on after the flames.
        PaintFrontLog(canvas, cx, floor, opening);
        PaintEmbers(canvas, cx, floor, opening, time);
        if (!CalmMotion)
        {
            PaintSparks(canvas, cx, floor, opening, time, shaken);
        }
    }

    protected override IEnumerable<SceneElement> ForegroundElements(int width, int height, float time)
    {
        var (cx, floor, hearth) = Geometry(width, height);
        var halfOpening = hearth * 0.46f;
        yield return new SceneElement(new SKRect(cx - halfOpening, floor - (hearth * 0.64f), cx + halfOpening, floor), tappedAt => _flamesTapped = tappedAt);

        var stockings = Stockings(width, height);
        for (var i = 0; i < stockings.Length; i++)
        {
            var index = i;
            var (x, y, w, h, _) = stockings[i];
            yield return new SceneElement(new SKRect(x - (w * 0.62f), y, x + (w * 1.45f), y + (h * 1.08f)), tappedAt => _stockingTapped[index] = tappedAt);
        }

        var presents = Presents(width, height);
        for (var i = 0; i < presents.Length; i++)
        {
            var index = i;
            var (x, bottom, size, _) = presents[i];
            yield return new SceneElement(new SKRect(x - (size / 2), bottom - (size * 1.1f), x + (size / 2), bottom), tappedAt => _presentTapped[index] = tappedAt);
        }
    }

    // Indoors, so no snow on the carpet.
    protected override void PaintSnow(SKCanvas canvas, int width, int height, float time)
    {
    }

    private (float X, float Floor, float Hearth) Geometry(int width, int height)
    {
        // Mantel and garland top out 1.12 hearths above the floor, and the presents reach 1.05 hearths to either side.
        var hearth = Math.Min(Math.Min(width * 0.47f, height * 0.5f), height * (ClearPart - 0.1f) / 1.18f);
        return (width / 2f, height * 0.9f, hearth);
    }

    private static void PaintWallpaper(SKCanvas canvas, int width, int height)
    {
        var unit = Math.Min(width, height) * 0.14f;
        using SKPaint paint = new() { IsAntialias = true, Color = SKColors.White.WithAlpha(12) };
        var row = 0;
        for (var y = unit / 2; y < height; y += unit, row++)
        {
            for (var x = row % 2 == 0 ? 0f : unit / 2; x < width + unit; x += unit)
            {
                Painter.Star(canvas, paint, x, y, unit * 0.18f, 6, 0.42f, 0);
            }
        }
    }

    // Square where it stands on the floor.
    private static SKRoundRect TopRounded(SKRect box, float radius)
    {
        SKRoundRect shape = new();
        SKPoint corner = new(radius, radius);
        shape.SetRectRadii(box, [corner, corner, SKPoint.Empty, SKPoint.Empty]);
        return shape;
    }

    private static void PaintBricks(SKCanvas canvas, float x, float y, float width, float height, float hearth)
    {
        using SKPaint paint = new() { Color = new SKColor(0x8F, 0x3F, 0x2D), StrokeWidth = Math.Max(1, hearth * 0.005f), IsAntialias = true };
        var rowHeight = hearth * 0.075f;
        var brick = hearth * 0.16f;
        var row = 0;
        for (var ly = y; ly < y + height; ly += rowHeight, row++)
        {
            if (row > 0)
            {
                canvas.DrawLine(x, ly, x + width, ly, paint);
            }

            for (var lx = x + (row % 2 == 0 ? brick / 2 : brick); lx < x + width; lx += brick)
            {
                canvas.DrawLine(lx, ly, lx, ly + rowHeight, paint);
            }
        }
    }

    // Both logs sit high enough that their tilted ends stay inside the firebox instead of poking out over the floor.
    private static void PaintBackLog(SKCanvas canvas, float cx, float floor, float opening) =>
        PaintLog(canvas, cx, floor, opening, degrees: -7, lift: 2f, Palette.WoodDark);

    // Low and nearly level, so it hides the foot of the flames and not the fire itself.
    private static void PaintFrontLog(SKCanvas canvas, float cx, float floor, float opening) =>
        PaintLog(canvas, cx, floor, opening, degrees: 2, lift: 1.15f, Palette.Wood);

    private static void PaintLog(SKCanvas canvas, float cx, float floor, float opening, float degrees, float lift, SKColor color)
    {
        using SKPaint paint = new() { IsAntialias = true, Color = color };
        var length = opening * 0.68f;
        var thick = opening * 0.09f;

        canvas.Save();
        canvas.RotateDegrees(degrees, cx, floor);
        canvas.DrawRoundRect(cx - (length / 2), floor - (thick * lift), length, thick, thick / 2, thick / 2, paint);
        canvas.Restore();
    }

    private static void PaintFlames(SKCanvas canvas, float cx, float baseY, float opening, float time, float roar)
    {
        SKColor[] colors = [Palette.Cranberry, Palette.Ember, Palette.Candle];
        float[] scales = [1f, 0.72f, 0.42f];
        using SKPaint paint = new() { IsAntialias = true };

        for (var layer = 0; layer < colors.Length; layer++)
        {
            var height = opening * 0.46f * scales[layer] * (1 + (roar * 0.55f));
            var width = opening * 0.62f * scales[layer] * (1 + (roar * 0.15f));
            using SKPath path = new();
            path.MoveTo(cx - (width / 2), baseY);

            const int Tongues = 5;
            for (var i = 0; i < Tongues; i++)
            {
                var t = (i + 0.5f) / Tongues;
                var bell = MathF.Sin(t * MathF.PI);
                var dance = 0.72f + (0.28f * MathF.Sin((time * (4.2f + i)) + (i * 2.1f) + layer));
                var tipX = cx + ((t - 0.5f) * width) + (MathF.Sin((time * 2.3f) + i) * width * 0.03f);
                var tipY = baseY - (height * bell * dance);
                var endX = cx + (((i + 1f) / Tongues - 0.5f) * width);
                var valley = baseY - (height * 0.28f * bell);
                path.QuadTo(tipX - (width * 0.05f), baseY - (height * bell * 0.4f), tipX, tipY);
                path.QuadTo(tipX + (width * 0.05f), baseY - (height * bell * 0.4f), endX, i == Tongues - 1 ? baseY : valley);
            }

            path.Close();
            paint.Color = colors[layer];
            canvas.DrawPath(path, paint);
        }
    }

    private static void PaintEmbers(SKCanvas canvas, float cx, float floor, float opening, float time)
    {
        Random random = new(31);
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < 16; i++)
        {
            var offset = random.NextSingle();
            var x = (random.NextSingle() - 0.5f) * opening * 0.6f;
            var speed = 0.14f + (random.NextSingle() * 0.2f);
            var progress = ((time * speed) + offset) % 1f;
            var y = floor - (opening * 0.1f) - (progress * opening * 0.55f);
            paint.Color = Palette.Candle.WithAlpha((byte)(255 * (1 - progress)));
            canvas.DrawCircle(cx + x + (MathF.Sin((time * 2) + i) * opening * 0.02f), y, opening * 0.006f, paint);
        }
    }

    // Sparks fly out of the fire, more and faster the harder it's shaken.
    private static void PaintSparks(SKCanvas canvas, float cx, float floor, float opening, float time, float shaken)
    {
        if (shaken <= 0)
        {
            return;
        }

        Random random = new(57);
        using SKPaint paint = new() { IsAntialias = true };
        var count = (int)(40 * shaken);
        for (var i = 0; i < count; i++)
        {
            var offset = random.NextSingle();
            var spread = (random.NextSingle() - 0.5f) * 2;
            var speed = 0.9f + (random.NextSingle() * 0.9f);
            var progress = ((time * speed) + offset) % 1f;
            var x = cx + (spread * opening * (0.15f + (0.45f * progress)));
            var y = floor - (opening * 0.15f) - (opening * 0.75f * progress) + (opening * 0.5f * progress * progress);
            paint.Color = (i % 3 == 0 ? Palette.Candle : Palette.Ember).WithAlpha((byte)(255 * (1 - progress) * Math.Min(1, shaken * 1.5f)));
            canvas.DrawCircle(x, y, opening * (0.006f + (0.006f * (1 - progress))), paint);
        }
    }

    private (float X, float Y, float Width, float Height, SKColor Color)[] Stockings(int width, int height)
    {
        var (cx, floor, hearth) = Geometry(width, height);
        var y = floor - (hearth * 1.02f) + (hearth * 0.02f);
        SKColor[] colors = [Palette.Cranberry, new(0x1F, 0x7A, 0x5A), new(0x2F, 0x6B, 0xC4)];
        return [.. colors.Select((color, i) => (cx + ((i - 1) * hearth * 0.4f), y, hearth * 0.095f, hearth * 0.2f, color))];
    }

    private void PaintStockings(SKCanvas canvas, int width, int height, float time)
    {
        var stockings = Stockings(width, height);
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < stockings.Length; i++)
        {
            var (x, y, w, h, color) = stockings[i];
            var tapped = Remaining(_stockingTapped[i], time, StockingReaction);
            var shaken = Shaken(time, ShakeSettle);
            var left = Math.Max(tapped, shaken);

            canvas.Save();
            if (left > 0 && CalmMotion)
            {
                Painter.Glow(canvas, x, y + (h / 2), h * 0.7f, Palette.Candle, (byte)(90 * left));
            }
            else if (tapped >= shaken && tapped > 0)
            {
                var age = (1 - tapped) * StockingReaction;
                canvas.RotateDegrees(StockingSwingDegrees * tapped * MathF.Sin(age * 7f), x, y);
            }
            else if (shaken > 0)
            {
                // Each on its own hook, so they swing out of step.
                canvas.RotateDegrees(StockingSwingDegrees * 1.3f * shaken * MathF.Sin((time * 9f) + (i * 1.9f)), x, y);
            }

            using SKPath sock = new();
            sock.MoveTo(x - (w / 2), y);
            sock.LineTo(x + (w / 2), y);
            sock.LineTo(x + (w / 2), y + (h * 0.72f));
            sock.QuadTo(x + (w * 1.45f), y + (h * 0.78f), x + (w * 1.3f), y + (h * 0.98f));
            sock.QuadTo(x + (w * 0.4f), y + (h * 1.08f), x - (w / 2), y + (h * 0.82f));
            sock.Close();
            paint.Color = color;
            canvas.DrawPath(sock, paint);

            paint.Color = Palette.Snow;
            canvas.DrawRoundRect(x - (w * 0.62f), y, w * 1.24f, h * 0.2f, w * 0.1f, w * 0.1f, paint);
            canvas.Restore();
        }
    }

    private static void PaintGarland(SKCanvas canvas, float cx, float y, float hearth)
    {
        using SKPaint paint = new() { IsAntialias = true };
        Random random = new(4);
        for (var i = 0; i < 44; i++)
        {
            var t = i / 43f;
            var x = cx + ((t - 0.5f) * hearth * 1.68f);
            paint.Color = i % 2 == 0 ? Palette.Pine : new SKColor(0x1F, 0x63, 0x55);
            canvas.DrawCircle(x, y + (random.NextSingle() * hearth * 0.012f), hearth * 0.028f, paint);
            if (i % 6 == 3)
            {
                paint.Color = Palette.Cranberry;
                canvas.DrawCircle(x, y, hearth * 0.016f, paint);
            }
        }
    }

    private (float X, float Bottom, float Size, SKColor Color)[] Presents(int width, int height)
    {
        var (cx, floor, hearth) = Geometry(width, height);
        var left = cx - (hearth * 0.75f);
        var right = cx + (hearth * 0.75f);
        return
        [
            (left - (hearth * 0.18f), floor + (hearth * 0.05f), hearth * 0.24f, new SKColor(0x2F, 0x6B, 0xC4)),
            (left, floor + (hearth * 0.08f), hearth * 0.15f, Palette.Cranberry),
            (right + (hearth * 0.15f), floor + (hearth * 0.06f), hearth * 0.2f, new SKColor(0x1F, 0x7A, 0x5A)),
        ];
    }

    private void PaintPresents(SKCanvas canvas, int width, int height, float time)
    {
        var presents = Presents(width, height);
        for (var i = 0; i < presents.Length; i++)
        {
            var (x, bottom, size, color) = presents[i];
            var tapped = Remaining(_presentTapped[i], time, PresentReaction);
            var shaken = Shaken(time, ShakeSettle);
            var left = Math.Max(tapped, shaken);
            var hop = 0f;
            if (left > 0 && CalmMotion)
            {
                Painter.Glow(canvas, x, bottom - (size / 2), size * 0.75f, Palette.Candle, (byte)(90 * left));
            }
            else if (tapped >= shaken && tapped > 0)
            {
                var age = (1 - tapped) * PresentReaction;
                hop = MathF.Abs(MathF.Sin(age * 6f)) * size * 0.4f * tapped;
            }
            else if (shaken > 0)
            {
                hop = MathF.Abs(MathF.Sin((time * 8f) + (i * 2.3f))) * size * 0.3f * shaken;
            }

            Box(canvas, x, bottom - hop, size, color);
        }
    }

    private static void Box(SKCanvas canvas, float x, float bottom, float size, SKColor color)
    {
        using SKPaint paint = new() { IsAntialias = true, Color = color };
        canvas.DrawRoundRect(x - (size / 2), bottom - size, size, size, size * 0.06f, size * 0.06f, paint);
        paint.Color = Palette.Candle;
        canvas.DrawRect(x - (size * 0.07f), bottom - size, size * 0.14f, size, paint);
        canvas.DrawRect(x - (size / 2), bottom - (size * 0.57f), size, size * 0.14f, paint);
        canvas.DrawOval(x - (size * 0.15f), bottom - size - (size * 0.04f), size * 0.15f, size * 0.07f, paint);
        canvas.DrawOval(x + (size * 0.15f), bottom - size - (size * 0.04f), size * 0.15f, size * 0.07f, paint);
    }
}
