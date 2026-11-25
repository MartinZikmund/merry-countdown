using SkiaSharp;

namespace MerryCountdown.Scenes;

internal static class Palette
{
    public static readonly SKColor Night = new(0x0B, 0x13, 0x2B);
    public static readonly SKColor Dusk = new(0x1C, 0x2A, 0x5E);
    public static readonly SKColor Haze = new(0x4A, 0x4E, 0x8F);
    public static readonly SKColor Snow = new(0xF1, 0xF6, 0xFF);
    public static readonly SKColor SnowShade = new(0xB9, 0xCB, 0xEC);
    public static readonly SKColor SnowDeep = new(0x7E, 0x95, 0xC9);
    public static readonly SKColor Pine = new(0x17, 0x4D, 0x45);
    public static readonly SKColor PineDark = new(0x0F, 0x33, 0x36);
    public static readonly SKColor Candle = new(0xFF, 0xC9, 0x5C);
    public static readonly SKColor Ember = new(0xFF, 0x7A, 0x2F);
    public static readonly SKColor Cranberry = new(0xD6, 0x28, 0x46);
    public static readonly SKColor Berry = new(0x8C, 0x17, 0x34);
    public static readonly SKColor Wood = new(0x6B, 0x3E, 0x2A);
    public static readonly SKColor WoodDark = new(0x41, 0x25, 0x1C);
    public static readonly SKColor Mint = new(0x5F, 0xE3, 0xB0);

    public static readonly SKColor[] SparkleColors = [Candle, Cranberry, Mint, Snow, new(0x7F, 0xC8, 0xFF)];
    public static readonly SKColor[] Lights = [Candle, Cranberry, Mint, new(0x7F, 0xC8, 0xFF), new(0xFF, 0x8A, 0xD8)];
}

internal static class Painter
{
    public static void Sky(SKCanvas canvas, int width, int height, params SKColor[] colors)
    {
        using var shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, height), colors, SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader };
        canvas.DrawRect(0, 0, width, height, paint);
    }

    public static void Glow(SKCanvas canvas, float x, float y, float radius, SKColor color, byte alpha = 150)
    {
        using var shader = SKShader.CreateRadialGradient(new SKPoint(x, y), Math.Max(radius, 0.1f), [color.WithAlpha(alpha), color.WithAlpha(0)], SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader, IsAntialias = true };
        canvas.DrawCircle(x, y, radius, paint);
    }

    public static void Star(SKCanvas canvas, SKPaint paint, float x, float y, float radius, int points, float inner, float rotation)
    {
        using SKPath path = new();
        for (var i = 0; i < points * 2; i++)
        {
            var r = i % 2 == 0 ? radius : radius * inner;
            var angle = rotation + (i * MathF.PI / points) - (MathF.PI / 2);
            var point = new SKPoint(x + (MathF.Cos(angle) * r), y + (MathF.Sin(angle) * r));
            if (i == 0)
            {
                path.MoveTo(point);
            }
            else
            {
                path.LineTo(point);
            }
        }

        path.Close();
        canvas.DrawPath(path, paint);
    }

    public static void Stars(SKCanvas canvas, int width, int height, float skyFraction, int seed, int count)
    {
        Random random = new(seed);
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < count; i++)
        {
            var x = random.NextSingle() * width;
            var y = random.NextSingle() * height * skyFraction;
            paint.Color = Palette.Snow.WithAlpha((byte)(70 + random.Next(170)));
            canvas.DrawCircle(x, y, 0.6f + (random.NextSingle() * Math.Min(width, height) * 0.0022f), paint);
        }
    }

    /// <summary>Only a few stars twinkle; animating the whole sky would be wasted on a software canvas.</summary>
    public static void Twinkles(SKCanvas canvas, int width, int height, float skyFraction, float time, int seed, int count)
    {
        Random random = new(seed);
        using SKPaint paint = new() { IsAntialias = true };
        var unit = Math.Min(width, height);
        for (var i = 0; i < count; i++)
        {
            var x = random.NextSingle() * width;
            var y = random.NextSingle() * height * skyFraction;
            var phase = random.NextSingle() * MathF.Tau;
            var speed = 0.6f + random.NextSingle();
            var brightness = 0.5f + (0.5f * MathF.Sin((time * speed) + phase));
            paint.Color = Palette.Snow.WithAlpha((byte)(255 * brightness));
            Star(canvas, paint, x, y, unit * 0.007f * (0.6f + brightness), 4, 0.25f, 0);
        }
    }

    public static void Moon(SKCanvas canvas, float x, float y, float radius)
    {
        Glow(canvas, x, y, radius * 4.2f, Palette.SnowShade, 90);
        using SKPaint paint = new() { IsAntialias = true, Color = new SKColor(0xFD, 0xF3, 0xD3) };
        canvas.DrawCircle(x, y, radius, paint);
        paint.Color = new SKColor(0xEF, 0xDF, 0xB4);
        canvas.DrawCircle(x - (radius * 0.35f), y - (radius * 0.2f), radius * 0.2f, paint);
        canvas.DrawCircle(x + (radius * 0.3f), y + (radius * 0.32f), radius * 0.26f, paint);
        canvas.DrawCircle(x + (radius * 0.28f), y - (radius * 0.42f), radius * 0.12f, paint);
    }

    /// <summary>A rolling snowy hill from one edge to the other; everything below it is filled.</summary>
    public static void Hill(SKCanvas canvas, int width, int height, float baseline, float swell, float waves, float phase, SKColor top, SKColor bottom)
    {
        using SKPath path = new();
        path.MoveTo(0, height);
        for (var x = 0; x <= width + 8; x += 8)
        {
            path.LineTo(x, HillY(x, width, baseline, swell, waves, phase));
        }

        path.LineTo(width, height);
        path.Close();

        using var shader = SKShader.CreateLinearGradient(new SKPoint(0, baseline - swell), new SKPoint(0, height), [top, bottom], SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader, IsAntialias = true };
        canvas.DrawPath(path, paint);
    }

    public static float HillY(float x, int width, float baseline, float swell, float waves, float phase) =>
        baseline - (MathF.Sin(((x / width) * waves * MathF.Tau) + phase) * swell);

    private const int PineTiers = 3;
    private const float PineTierStep = 0.26f;
    private const float PineTierTall = 0.46f;
    private const float PineTierWaist = 0.08f;

    private static float PineTierHalf(int tier) => 0.25f * (0.45f + (tier / (float)PineTiers * 0.55f));

    /// <summary>Half the width of a pine's branches at a depth below its tip, both as shares of its height.</summary>
    public static float PineHalfWidth(float depth)
    {
        var widest = 0f;
        for (var tier = 0; tier < PineTiers; tier++)
        {
            var into = depth - (PineTierStep * tier);
            if (into < 0 || into > PineTierTall)
            {
                continue;
            }

            // The edge is the curve Pine draws: its width grows evenly along it while its depth bows inwards.
            var (a, b) = (PineTierTall - (2 * PineTierWaist), 2 * (PineTierTall - PineTierWaist));
            var along = (b - MathF.Sqrt(Math.Max(0, (b * b) - (4 * a * into)))) / (2 * a);
            widest = Math.Max(widest, PineTierHalf(tier) * along);
        }

        return widest;
    }

    public static void Pine(SKCanvas canvas, float x, float groundY, float height, SKColor color, SKColor snowColor)
    {
        using SKPaint paint = new() { IsAntialias = true, Color = Palette.WoodDark };
        canvas.DrawRect(x - (height * 0.035f), groundY - (height * 0.14f), height * 0.07f, height * 0.16f, paint);

        for (var tier = 0; tier < PineTiers; tier++)
        {
            var top = groundY - height + (height * PineTierStep * tier);
            var bottom = top + (height * PineTierTall);
            var half = height * PineTierHalf(tier);

            using SKPath body = new();
            body.MoveTo(x, top);
            body.QuadTo(x + (half * 0.5f), bottom - (height * PineTierWaist), x + half, bottom);
            body.QuadTo(x, bottom + (height * 0.05f), x - half, bottom);
            body.QuadTo(x - (half * 0.5f), bottom - (height * PineTierWaist), x, top);
            body.Close();
            paint.Color = color;
            canvas.DrawPath(body, paint);

            using SKPath cap = new();
            var capBottom = top + (height * 0.19f);
            cap.MoveTo(x, top);
            cap.QuadTo(x + (half * 0.34f), capBottom - (height * 0.05f), x + (half * 0.5f), capBottom);
            cap.QuadTo(x + (half * 0.2f), capBottom - (height * 0.035f), x, capBottom + (height * 0.01f));
            cap.QuadTo(x - (half * 0.2f), capBottom - (height * 0.035f), x - (half * 0.5f), capBottom);
            cap.QuadTo(x - (half * 0.34f), capBottom - (height * 0.05f), x, top);
            cap.Close();
            paint.Color = snowColor;
            canvas.DrawPath(cap, paint);
        }
    }

    /// <summary>The windows go on before the roof, so their glow warms the wall and leaves the snow above it white.</summary>
    public static void House(SKCanvas canvas, float x, float groundY, float width, float height, SKColor wall, SKColor roofSnow, ReadOnlySpan<SKRect> windows, SKColor light)
    {
        using SKPaint paint = new() { IsAntialias = true, Color = wall };
        var wallTop = groundY - height;
        canvas.DrawRoundRect(x - (width / 2), wallTop, width, height + 4, width * 0.03f, width * 0.03f, paint);
        foreach (var window in windows)
        {
            Window(canvas, window.Left, window.Top, window.Width, window.Height, light);
        }

        var overhang = width * 0.14f;
        var roofHeight = width * 0.46f;
        using SKPath roof = new();
        roof.MoveTo(x - (width / 2) - overhang, wallTop + (roofHeight * 0.12f));
        roof.QuadTo(x - (width * 0.2f), wallTop - (roofHeight * 0.55f), x, wallTop - roofHeight);
        roof.QuadTo(x + (width * 0.2f), wallTop - (roofHeight * 0.55f), x + (width / 2) + overhang, wallTop + (roofHeight * 0.12f));
        roof.QuadTo(x, wallTop - (roofHeight * 0.1f), x - (width / 2) - overhang, wallTop + (roofHeight * 0.12f));
        roof.Close();
        paint.Color = roofSnow;
        canvas.DrawPath(roof, paint);
    }

    public static void Window(SKCanvas canvas, float x, float y, float width, float height, SKColor light)
    {
        Glow(canvas, x + (width / 2), y + (height / 2), width * 2.1f, light, 110);
        WindowPane(canvas, x, y, width, height, light);
    }

    public static void WindowPane(SKCanvas canvas, float x, float y, float width, float height, SKColor light)
    {
        using SKPaint paint = new() { IsAntialias = true, Color = light };
        canvas.DrawRoundRect(x, y, width, height, width * 0.12f, width * 0.12f, paint);
        paint.Color = Palette.WoodDark;
        canvas.DrawRect(x + (width * 0.46f), y, width * 0.08f, height, paint);
        canvas.DrawRect(x, y + (height * 0.46f), width, height * 0.08f, paint);
    }

    public static void Vignette(SKCanvas canvas, int width, int height, byte alpha)
    {
        using var shader = SKShader.CreateRadialGradient(new SKPoint(width / 2f, height / 2f), Math.Max(width, height) * 0.75f, [SKColors.Transparent, SKColors.Black.WithAlpha(alpha)], [0.55f, 1f], SKShaderTileMode.Clamp);
        using SKPaint paint = new() { Shader = shader };
        canvas.DrawRect(0, 0, width, height, paint);
    }
}

/// <summary>Flakes are computed from time alone, so snow needs no per-frame state or allocations.</summary>
internal sealed class SnowField
{
    private const int GentleFlakes = 110;
    private const int BlizzardFlakes = 340;

    private readonly Flake[] _flakes = CreateFlakes();

    public void Paint(SKCanvas canvas, int width, int height, float time, SnowAmount amount)
    {
        if (amount == SnowAmount.None)
        {
            return;
        }

        var scale = MathF.Min(1.6f, MathF.Sqrt(width * (float)height) / 900f);
        var count = (int)((amount == SnowAmount.Blizzard ? BlizzardFlakes : GentleFlakes) * scale);
        var wind = amount == SnowAmount.Blizzard ? 0.22f : 0.05f;
        var pace = amount == SnowAmount.Blizzard ? 1.7f : 1f;
        var unit = Math.Min(width, height);

        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < count && i < _flakes.Length; i++)
        {
            ref readonly var flake = ref _flakes[i];
            var progress = ((time * flake.Speed * pace) + flake.Offset) % 1f;
            var y = progress * (height + 20) - 10;
            var drift = (MathF.Sin((time * flake.Sway) + flake.Phase) * 0.012f) + (progress * wind);
            var x = ((flake.X + drift) % 1f) * width;
            paint.Color = Palette.Snow.WithAlpha(flake.Alpha);
            canvas.DrawCircle(x, y, flake.Size * unit, paint);
        }
    }

    private static Flake[] CreateFlakes()
    {
        Random random = new(1224);
        var flakes = new Flake[BlizzardFlakes * 2];
        for (var i = 0; i < flakes.Length; i++)
        {
            var depth = random.NextSingle();
            flakes[i] = new Flake(
                random.NextSingle(),
                random.NextSingle(),
                0.03f + (depth * 0.07f),
                0.3f + random.NextSingle(),
                random.NextSingle() * MathF.Tau,
                0.0012f + (depth * 0.0034f),
                (byte)(120 + (depth * 135)));
        }

        return flakes;
    }

    private readonly record struct Flake(float X, float Offset, float Speed, float Sway, float Phase, float Size, byte Alpha);
}
