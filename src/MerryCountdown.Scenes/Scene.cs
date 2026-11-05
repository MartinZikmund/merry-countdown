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
    private const float CrossfadeSeconds = 1.2f;
    private const float ShadedPart = 0.6f;
    private const byte ShadeAlpha = 140;

    private SKImage? _backdrop;
    private SKSizeI _backdropSize;
    private readonly SnowField _snow = new();
    private readonly List<Sparkle> _sparkles = [];
    private SKImage? _photo;
    private bool _showForeground = true;
    private SKImage? _fadingOut;
    private float? _fadeStart;
    private PhotoChange? _pendingPhotoSlot;

    public abstract string Id { get; }

    public SnowAmount Snow { get; set; } = SnowAmount.Gentle;

    /// <summary>
    /// Shows a family photo instead of the painted sky, optionally with the scene's foreground still in front.
    /// The caller keeps owning the image. Null brings the painted scene back.
    /// </summary>
    public void SetPhoto(SKImage? photo, bool showForeground) => Volatile.Write(ref _pendingPhotoSlot, new PhotoChange(photo, showForeground));

    // Rendering is software on some heads, so whatever doesn't move is painted once and reused.
    protected abstract void PaintSky(SKCanvas canvas, int width, int height);

    protected virtual void PaintForeground(SKCanvas canvas, int width, int height)
    {
    }

    protected virtual void PaintSkyLife(SKCanvas canvas, int width, int height, float time)
    {
    }

    protected virtual void PaintForegroundLife(SKCanvas canvas, int width, int height, float time)
    {
    }

    public void Render(SKCanvas canvas, int width, int height, float time)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        ApplyPendingPhoto();
        EnsureBackdrop(width, height);
        canvas.DrawImage(_backdrop!, 0, 0);
        PaintFadingOut(canvas, time);

        if (_photo is null)
        {
            PaintSkyLife(canvas, width, height, time);
        }

        if (_photo is null || _showForeground)
        {
            PaintForegroundLife(canvas, width, height, time);
        }

        // Scenes without weather of their own (indoors, the globe) still get it over a bare photo.
        if (_photo is not null && !_showForeground)
        {
            _snow.Paint(canvas, width, height, time, Snow);
        }
        else
        {
            PaintSnow(canvas, width, height, time);
        }

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
        _fadingOut?.Dispose();
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
        PaintStill(surface.Canvas, width, height);
        _backdrop = surface.Snapshot();
        _backdropSize = new SKSizeI(width, height);
    }

    // Some heads render off the UI thread, so a new photo only takes effect between frames.
    private void ApplyPendingPhoto()
    {
        if (Interlocked.Exchange(ref _pendingPhotoSlot, null) is not { } change)
        {
            return;
        }

        if (ReferenceEquals(_photo, change.Photo) && _showForeground == change.ShowForeground)
        {
            return;
        }

        _photo = change.Photo;
        _showForeground = change.ShowForeground;

        _fadingOut?.Dispose();
        _fadingOut = _backdrop;
        _fadeStart = null;
        _backdrop = null;
    }

    private void PaintStill(SKCanvas canvas, int width, int height)
    {
        if (_photo is null)
        {
            PaintSky(canvas, width, height);
            PaintForeground(canvas, width, height);
            return;
        }

        PaintPhoto(canvas, _photo, width, height);
        if (_showForeground)
        {
            PaintForeground(canvas, width, height);
        }
    }

    private static void PaintPhoto(SKCanvas canvas, SKImage photo, int width, int height)
    {
        var scale = Math.Max(width / (float)photo.Width, height / (float)photo.Height);
        var drawnWidth = photo.Width * scale;
        var drawnHeight = photo.Height * scale;
        SKRect target = SKRect.Create((width - drawnWidth) / 2, (height - drawnHeight) / 2, drawnWidth, drawnHeight);
        canvas.DrawImage(photo, target, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));

        // The countdown is white and sits on top, and photos are often bright up there.
        using var shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, height * ShadedPart), [SKColors.Black.WithAlpha(ShadeAlpha), SKColors.Transparent], SKShaderTileMode.Clamp);
        using SKPaint shade = new() { Shader = shader };
        canvas.DrawRect(0, 0, width, height, shade);
    }

    private void PaintFadingOut(SKCanvas canvas, float time)
    {
        if (_fadingOut is null)
        {
            return;
        }

        _fadeStart ??= time;
        var progress = (time - _fadeStart.Value) / CrossfadeSeconds;
        if (progress >= 1)
        {
            _fadingOut.Dispose();
            _fadingOut = null;
            return;
        }

        using SKPaint paint = new() { Color = SKColors.White.WithAlpha((byte)(255 * (1 - progress))) };
        canvas.DrawImage(_fadingOut, 0, 0, paint);
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

    private sealed record PhotoChange(SKImage? Photo, bool ShowForeground);

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
