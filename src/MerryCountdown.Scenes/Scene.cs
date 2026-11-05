using SkiaSharp;

namespace MerryCountdown.Scenes;

public enum SnowAmount
{
    None,
    Gentle,
    Blizzard,
}

/// <summary>
/// A living picture behind the countdown. Time goes in, pixels come out: the same time always draws
/// the same frame (bursts aside), which is what makes scenes checkable without a window.
/// </summary>
public abstract class Scene : IDisposable
{
    private SKImage? _backdrop;
    private SKSizeI _backdropSize;
    private readonly SnowField _snow = new();
    private readonly List<Sparkle> _sparkles = [];

    public abstract string Id { get; }

    public SnowAmount Snow { get; set; } = SnowAmount.Gentle;

    /// <summary>Rendering is software on some heads, so whatever doesn't move is painted once and reused.</summary>
    protected abstract void PaintBackdrop(SKCanvas canvas, int width, int height);

    protected abstract void PaintLife(SKCanvas canvas, int width, int height, float time);

    public void Render(SKCanvas canvas, int width, int height, float time)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        EnsureBackdrop(width, height);
        canvas.DrawImage(_backdrop!, 0, 0);
        PaintLife(canvas, width, height, time);
        PaintSnow(canvas, width, height, time);
        PaintSparkles(canvas, width, height, time);
    }

    /// <summary>A tap anywhere makes the picture answer back.</summary>
    public virtual void Tap(float x, float y, float time)
    {
        Random random = new(HashCode.Combine(x, y, time));
        for (var i = 0; i < 26; i++)
        {
            var angle = random.NextSingle() * MathF.Tau;
            var speed = 0.12f + (random.NextSingle() * 0.42f);
            _sparkles.Add(new Sparkle(x, y, MathF.Cos(angle) * speed, MathF.Sin(angle) * speed, time, Palette.SparkleColors[i % Palette.SparkleColors.Length]));
        }

        if (_sparkles.Count > 300)
        {
            _sparkles.RemoveRange(0, _sparkles.Count - 300);
        }
    }

    public void Dispose()
    {
        _backdrop?.Dispose();
        GC.SuppressFinalize(this);
    }

    protected virtual void PaintSnow(SKCanvas canvas, int width, int height, float time) =>
        _snow.Paint(canvas, width, height, time, Snow);

    private void EnsureBackdrop(int width, int height)
    {
        if (_backdrop is not null && _backdropSize.Width == width && _backdropSize.Height == height)
        {
            return;
        }

        _backdrop?.Dispose();
        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        PaintBackdrop(surface.Canvas, width, height);
        _backdrop = surface.Snapshot();
        _backdropSize = new SKSizeI(width, height);
    }

    private void PaintSparkles(SKCanvas canvas, int width, int height, float time)
    {
        const float Life = 1.1f;
        _sparkles.RemoveAll(s => time - s.Born > Life);

        using SKPaint paint = new() { IsAntialias = true };
        var unit = Math.Min(width, height);
        foreach (var sparkle in _sparkles)
        {
            var age = time - sparkle.Born;
            var fade = 1 - (age / Life);
            var x = sparkle.X + (sparkle.Vx * age * unit);
            var y = sparkle.Y + (((sparkle.Vy * age) + (0.25f * age * age)) * unit);
            paint.Color = sparkle.Color.WithAlpha((byte)(255 * fade));
            Painter.Star(canvas, paint, x, y, unit * 0.016f * fade, 5, 0.45f, age * 3);
        }
    }

    private readonly record struct Sparkle(float X, float Y, float Vx, float Vy, float Born, SKColor Color);

    public static IReadOnlyList<string> AllIds { get; } = ["village", "globe", "aurora", "fireplace", "train"];

    public static Scene Create(string? id) => id switch
    {
        "globe" => new SnowGlobeScene(),
        "aurora" => new AuroraScene(),
        "fireplace" => new FireplaceScene(),
        "train" => new ToyTrainScene(),
        _ => new VillageScene(),
    };
}
