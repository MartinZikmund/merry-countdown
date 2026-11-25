using SkiaSharp;

namespace MerryCountdown.Scenes;

/// <summary>Something in a scene that answers a tap. The area is what's painted; <see cref="Scene"/> adds the margin for small fingers.</summary>
public sealed record SceneElement(SKRect Area, Action<float> React, bool IsRound = false);

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
    private const float TapMargin = 0.025f;
    private const float MostCovered = 0.6f;
    private const float FlurrySeconds = 2.5f;
    private const int FlurryFlakes = 160;

    private SKImage? _backdrop;
    private SKSizeI _backdropSize;
    private float _backdropCover;
    private float _coveredTop;
    private readonly SnowField _snow = new();
    private readonly List<Sparkle> _sparkles = [];
    private SKImage? _photo;
    private bool _showForeground = true;
    private SKImage? _fadingOut;
    private float? _fadeStart;
    private PhotoChange? _pendingPhotoSlot;
    private float _shakenAt = float.NegativeInfinity;
    private float _shakeStrength;
    private bool _isDisposed;

    public abstract string Id { get; }

    public SnowAmount Snow { get; set; } = SnowAmount.Gentle;

    /// <summary>Reactions light things up instead of moving them.</summary>
    public bool CalmMotion { get; set; }

    /// <summary>
    /// How much of the height the sign hangs over, from the top. Scenes with a centrepiece shrink it to fit below.
    /// </summary>
    public float CoveredTop
    {
        get => _coveredTop;
        set => _coveredTop = float.IsFinite(value) ? Math.Clamp(value, 0, MostCovered) : 0;
    }

    /// <summary>The share of the height still in plain view.</summary>
    protected float ClearPart => 1 - _coveredTop;

    internal int SparkleCount => _sparkles.Count;

    internal bool IsDisposed => _isDisposed;

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

    /// <summary>What can be tapped in the foreground, front to back, worked out from the same layout that paints it.</summary>
    protected virtual IEnumerable<SceneElement> ForegroundElements(int width, int height, float time) => [];

    /// <summary>What can be tapped in the sky, which a photo replaces.</summary>
    protected virtual IEnumerable<SceneElement> SkyElements(int width, int height, float time) => [];

    /// <summary>How much of a reaction is left: 1 at the tap, down to 0 after the given time.</summary>
    protected static float Remaining(float tappedAt, float time, float seconds)
    {
        var age = time - tappedAt;
        return age >= 0 && age < seconds ? 1 - (age / seconds) : 0;
    }

    /// <summary>How much of a shake is left: its strength while it lasts, down to 0 the given time after the last push.</summary>
    protected float Shaken(float time, float seconds) => _shakeStrength * Remaining(_shakenAt, time, seconds);

    public void Render(SKCanvas canvas, int width, int height, float time)
    {
        if (width <= 0 || height <= 0 || _isDisposed)
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
            PaintFlurry(canvas, width, height, time);
        }
        else
        {
            PaintSnow(canvas, width, height, time);
        }

        PaintSparkles(canvas, width, height, time);
    }

    /// <summary>A tap anywhere makes the picture answer back: the element under it reacts, otherwise a burst of stars.</summary>
    /// <returns>Whether an element reacted.</returns>
    public bool Tap(float x, float y, int width, int height, float time)
    {
        if (ElementAt(x, y, width, height, time) is { } element)
        {
            element.React(time);
            return true;
        }

        ThrowSparkles(x, y, time);
        return false;
    }

    /// <summary>
    /// The device or the window is being shaken. Called for every push of the shake, so the scene follows it while
    /// it lasts and settles once the pushes stop.
    /// </summary>
    /// <param name="strength">From just over 0 for a gentle shake to 1 for a wild one.</param>
    public void Shake(float strength, float time)
    {
        strength = float.IsFinite(strength) ? Math.Clamp(strength, 0, 1) : 0;

        // Within one shake it only ever gets wilder, so a weaker push doesn't make the scene flinch.
        _shakeStrength = Remaining(_shakenAt, time, 1) > 0 ? Math.Max(_shakeStrength, strength) : strength;
        _shakenAt = time;
    }

    internal SceneElement? ElementAt(float x, float y, int width, int height, float time)
    {
        var margin = Math.Min(width, height) * TapMargin;
        return VisibleElements(width, height, time).FirstOrDefault(e => Contains(e, x, y, margin));
    }

    internal IEnumerable<SceneElement> VisibleElements(int width, int height, float time)
    {
        // A photo set a moment ago counts already, even if no frame has picked it up yet.
        var (photo, showForeground) = Volatile.Read(ref _pendingPhotoSlot) is { } change
            ? (change.Photo, change.ShowForeground)
            : (_photo, _showForeground);

        var foreground = photo is null || showForeground ? ForegroundElements(width, height, time) : [];
        var sky = photo is null ? SkyElements(width, height, time) : [];
        SKRect screen = SKRect.Create(width, height);
        return foreground.Concat(sky).Where(e => e.Area.IntersectsWith(screen));
    }

    private static bool Contains(SceneElement element, float x, float y, float margin)
    {
        var area = element.Area;
        var (halfWidth, halfHeight) = ((area.Width / 2) + margin, (area.Height / 2) + margin);
        var (dx, dy) = ((x - area.MidX) / halfWidth, (y - area.MidY) / halfHeight);
        return element.IsRound ? (dx * dx) + (dy * dy) <= 1 : MathF.Abs(dx) <= 1 && MathF.Abs(dy) <= 1;
    }

    private void ThrowSparkles(float x, float y, float time)
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
        _isDisposed = true;
        _backdrop?.Dispose();
        _fadingOut?.Dispose();
        Release();
        GC.SuppressFinalize(this);
    }

    /// <summary>For a scene to free what it keeps between frames.</summary>
    protected virtual void Release()
    {
    }

    protected virtual void PaintSnow(SKCanvas canvas, int width, int height, float time) =>
        _snow.Paint(canvas, width, height, time, Snow);

    private void EnsureBackdrop(int width, int height)
    {
        var cover = _coveredTop;
        if (_backdrop is not null && _backdropSize.Width == width && _backdropSize.Height == height && _backdropCover == cover)
        {
            return;
        }

        _backdrop?.Dispose();
        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        PaintStill(surface.Canvas, width, height);
        _backdrop = surface.Snapshot();
        _backdropSize = new SKSizeI(width, height);
        _backdropCover = cover;
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

    // A bare photo has nothing of its own to shake, so it gets a burst of snow.
    private void PaintFlurry(SKCanvas canvas, int width, int height, float time)
    {
        var shake = Shaken(time, FlurrySeconds);
        if (shake <= 0)
        {
            return;
        }

        var unit = Math.Min(width, height);
        var count = (int)(FlurryFlakes * shake);
        var swirl = CalmMotion ? 0 : 1;
        Random random = new(25);
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < count; i++)
        {
            var (seedX, seedY) = (random.NextSingle(), random.NextSingle());
            var speed = 0.15f + (random.NextSingle() * 0.35f);
            var phase = random.NextSingle() * MathF.Tau;
            var size = unit * (0.003f + (random.NextSingle() * 0.006f));

            var x = (seedX + (swirl * 0.08f * MathF.Sin((time * 3) + phase))) * width;
            var y = ((seedY + (time * speed * (CalmMotion ? 0.3f : 1))) % 1f) * height;
            paint.Color = SKColors.White.WithAlpha((byte)(230 * Math.Min(1, shake * 1.5f) * (CalmMotion ? 0.5f : 1)));
            canvas.DrawCircle(x, y, size, paint);
        }
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
