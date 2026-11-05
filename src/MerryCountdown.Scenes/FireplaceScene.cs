using SkiaSharp;

namespace MerryCountdown.Scenes;

public sealed class FireplaceScene : Scene
{
    public override string Id => "fireplace";

    protected override void PaintBackdrop(SKCanvas canvas, int width, int height)
    {
        Painter.Sky(canvas, width, height, new SKColor(0x2A, 0x0F, 0x24), new SKColor(0x4A, 0x18, 0x2C), new SKColor(0x6E, 0x26, 0x2C));
        PaintWallpaper(canvas, width, height);

        var (cx, floor, hearth) = Geometry(width, height);
        using SKPaint paint = new() { IsAntialias = true };

        using var floorShader = SKShader.CreateLinearGradient(new SKPoint(0, floor), new SKPoint(0, height), [Palette.Wood, Palette.WoodDark], SKShaderTileMode.Clamp);
        paint.Shader = floorShader;
        canvas.DrawRect(0, floor, width, height - floor, paint);
        paint.Shader = null;

        var outerWidth = hearth * 1.5f;
        var outerHeight = hearth * 1.02f;
        paint.Color = new SKColor(0xB4, 0x57, 0x3E);
        canvas.DrawRoundRect(cx - (outerWidth / 2), floor - outerHeight, outerWidth, outerHeight, hearth * 0.02f, hearth * 0.02f, paint);
        PaintBricks(canvas, cx - (outerWidth / 2), floor - outerHeight, outerWidth, outerHeight, hearth);

        paint.Color = new SKColor(0x1A, 0x0C, 0x10);
        var opening = hearth * 0.92f;
        using SKPath arch = new();
        arch.AddRoundRect(new SKRect(cx - (opening / 2), floor - (opening * 0.7f), cx + (opening / 2), floor + 4), opening * 0.2f, opening * 0.2f);
        canvas.DrawPath(arch, paint);

        paint.Color = Palette.Wood;
        canvas.DrawRoundRect(cx - (hearth * 0.86f), floor - outerHeight - (hearth * 0.07f), hearth * 1.72f, hearth * 0.09f, hearth * 0.015f, hearth * 0.015f, paint);

        PaintLogs(canvas, cx, floor, opening);
        PaintStockings(canvas, cx, floor - outerHeight + (hearth * 0.02f), hearth);
        PaintGarland(canvas, cx, floor - outerHeight - (hearth * 0.07f), hearth);
        PaintPresents(canvas, width, floor, hearth);
    }

    protected override void PaintLife(SKCanvas canvas, int width, int height, float time)
    {
        var (cx, floor, hearth) = Geometry(width, height);
        var opening = hearth * 0.92f;
        var flicker = 0.85f + (0.15f * MathF.Sin(time * 7.3f) * MathF.Sin(time * 3.1f));
        Painter.Glow(canvas, cx, floor - (opening * 0.2f), hearth * 1.5f * flicker, Palette.Ember, 95);
        PaintFlames(canvas, cx, floor - (opening * 0.07f), opening, time);
        PaintEmbers(canvas, cx, floor, opening, time);
    }

    // Indoors, so no snow on the carpet.
    protected override void PaintSnow(SKCanvas canvas, int width, int height, float time)
    {
    }

    private static (float X, float Floor, float Hearth) Geometry(int width, int height)
    {
        var hearth = Math.Min(width * 0.62f, height * 0.5f);
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

    private static void PaintBricks(SKCanvas canvas, float x, float y, float width, float height, float hearth)
    {
        using SKPaint paint = new() { Color = new SKColor(0x8F, 0x3F, 0x2D), StrokeWidth = Math.Max(1, hearth * 0.005f), IsAntialias = true };
        var rowHeight = hearth * 0.075f;
        var brick = hearth * 0.16f;
        var row = 0;
        for (var ly = y + rowHeight; ly < y + height; ly += rowHeight, row++)
        {
            canvas.DrawLine(x, ly, x + width, ly, paint);
            for (var lx = x + (row % 2 == 0 ? 0 : brick / 2); lx < x + width; lx += brick)
            {
                canvas.DrawLine(lx, ly, lx, ly + rowHeight, paint);
            }
        }
    }

    private static void PaintLogs(SKCanvas canvas, float cx, float floor, float opening)
    {
        using SKPaint paint = new() { IsAntialias = true, Color = Palette.WoodDark };
        var length = opening * 0.68f;
        var thick = opening * 0.09f;
        canvas.Save();
        canvas.RotateDegrees(-7, cx, floor);
        canvas.DrawRoundRect(cx - (length / 2), floor - (thick * 1.55f), length, thick, thick / 2, thick / 2, paint);
        canvas.Restore();
        canvas.Save();
        canvas.RotateDegrees(8, cx, floor);
        paint.Color = Palette.Wood;
        canvas.DrawRoundRect(cx - (length / 2), floor - (thick * 1.1f), length, thick, thick / 2, thick / 2, paint);
        canvas.Restore();
    }

    private static void PaintFlames(SKCanvas canvas, float cx, float baseY, float opening, float time)
    {
        SKColor[] colors = [Palette.Cranberry, Palette.Ember, Palette.Candle];
        float[] scales = [1f, 0.72f, 0.42f];
        using SKPaint paint = new() { IsAntialias = true };

        for (var layer = 0; layer < colors.Length; layer++)
        {
            var height = opening * 0.46f * scales[layer];
            var width = opening * 0.62f * scales[layer];
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

    private static void PaintStockings(SKCanvas canvas, float cx, float y, float hearth)
    {
        SKColor[] colors = [Palette.Cranberry, new(0x1F, 0x7A, 0x5A), new(0x2F, 0x6B, 0xC4)];
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < colors.Length; i++)
        {
            var x = cx + ((i - 1) * hearth * 0.4f);
            var w = hearth * 0.095f;
            var h = hearth * 0.2f;

            using SKPath sock = new();
            sock.MoveTo(x - (w / 2), y);
            sock.LineTo(x + (w / 2), y);
            sock.LineTo(x + (w / 2), y + (h * 0.72f));
            sock.QuadTo(x + (w * 1.45f), y + (h * 0.78f), x + (w * 1.3f), y + (h * 0.98f));
            sock.QuadTo(x + (w * 0.4f), y + (h * 1.08f), x - (w / 2), y + (h * 0.82f));
            sock.Close();
            paint.Color = colors[i];
            canvas.DrawPath(sock, paint);

            paint.Color = Palette.Snow;
            canvas.DrawRoundRect(x - (w * 0.62f), y, w * 1.24f, h * 0.2f, w * 0.1f, w * 0.1f, paint);
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

    private static void PaintPresents(SKCanvas canvas, int width, float floor, float hearth)
    {
        SKColor[] boxes = [new(0x2F, 0x6B, 0xC4), Palette.Cranberry, new(0x1F, 0x7A, 0x5A)];
        var left = (width / 2f) - (hearth * 0.75f);
        var right = (width / 2f) + (hearth * 0.75f);

        Box(canvas, left - (hearth * 0.18f), floor + (hearth * 0.05f), hearth * 0.24f, boxes[0]);
        Box(canvas, left + (hearth * 0.0f), floor + (hearth * 0.08f), hearth * 0.15f, boxes[1]);
        Box(canvas, right + (hearth * 0.15f), floor + (hearth * 0.06f), hearth * 0.2f, boxes[2]);
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
