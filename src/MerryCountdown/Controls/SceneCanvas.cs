using System.Diagnostics;
using MerryCountdown.Core.Family;
using MerryCountdown.Scenes;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp;
#if WINAPPSDK
using SkiaSharp.Views.Windows;
#else
using Uno.WinUI.Graphics2DSK;
#endif

namespace MerryCountdown.Controls;

/// <summary>Hosts a <see cref="Scene"/>. The one place that knows how each head gets a Skia canvas.</summary>
/// <remarks>
/// A Grid on purpose: a transparent Panel background is hit-testable, which is what makes tapping work.
/// Uno heads draw straight into the window through SKCanvasElement. WinUI uses SkiaSharp's own views:
/// the GPU-backed swap chain for opaque scenes, the XAML canvas for see-through ones.
/// </remarks>
public sealed partial class SceneCanvas : Grid
{
    private const double FramesPerSecond = 30;
    private const double CalmFramesPerSecond = 10;
    private const float CalmTimeScale = 0.35f;

    public static readonly DependencyProperty SceneIdProperty = DependencyProperty.Register(
        nameof(SceneId), typeof(string), typeof(SceneCanvas), new PropertyMetadata(null, (d, _) => ((SceneCanvas)d).ReplaceScene()));

    public static readonly DependencyProperty SnowProperty = DependencyProperty.Register(
        nameof(Snow), typeof(SnowLevel), typeof(SceneCanvas), new PropertyMetadata(SnowLevel.Gentle, (d, _) => ((SceneCanvas)d).ApplySnow()));

    public static readonly DependencyProperty CalmMotionProperty = DependencyProperty.Register(
        nameof(CalmMotion), typeof(bool), typeof(SceneCanvas), new PropertyMetadata(false, (d, _) => ((SceneCanvas)d).ApplyCalmMotion()));

    public static readonly DependencyProperty PhotoProperty = DependencyProperty.Register(
        nameof(Photo), typeof(byte[]), typeof(SceneCanvas), new PropertyMetadata(null, (d, _) => ((SceneCanvas)d).DecodePhoto()));

    public static readonly DependencyProperty ShowForegroundProperty = DependencyProperty.Register(
        nameof(ShowForeground), typeof(bool), typeof(SceneCanvas), new PropertyMetadata(true, (d, _) => ((SceneCanvas)d).ApplyPhoto()));

    public static readonly DependencyProperty IsPausedProperty = DependencyProperty.Register(
        nameof(IsPaused), typeof(bool), typeof(SceneCanvas), new PropertyMetadata(false));

    private readonly Stopwatch _clock = new();
    private readonly Action _invalidate;
    private Scene? _scene;
    private SKImage? _photoImage;
    private TimeSpan _lastFrame;
    private float _sceneTime;
    private float _pixelsPerDip = 1;
    private bool _isRunning;

    public SceneCanvas()
        : this(isTransparent: false)
    {
    }

    private SceneCanvas(bool isTransparent)
    {
        Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

#if WINAPPSDK
        if (isTransparent)
        {
            SKXamlCanvas canvas = new();
            canvas.PaintSurface += (_, e) =>
            {
                e.Surface.Canvas.Clear(SKColors.Transparent);
                Paint(e.Surface.Canvas, canvas.CanvasSize.Width, canvas.CanvasSize.Height);
            };
            Children.Add(canvas);
            _invalidate = canvas.Invalidate;
        }
        else
        {
            SKSwapChainPanel panel = new() { EnableRenderLoop = false };
            panel.PaintSurface += (_, e) => Paint(e.Surface.Canvas, panel.CanvasSize.Width, panel.CanvasSize.Height);
            Children.Add(panel);
            _invalidate = panel.Invalidate;
        }
#else
        SceneElement element = new(this);
        Children.Add(element);
        _invalidate = element.Invalidate;
#endif

        Loaded += (_, _) => Start();
        Unloaded += (_, _) => Stop();
        Tapped += OnTapped;
    }

    public string? SceneId
    {
        get => (string?)GetValue(SceneIdProperty);
        set => SetValue(SceneIdProperty, value);
    }

    public SnowLevel Snow
    {
        get => (SnowLevel)GetValue(SnowProperty);
        set => SetValue(SnowProperty, value);
    }

    public bool CalmMotion
    {
        get => (bool)GetValue(CalmMotionProperty);
        set => SetValue(CalmMotionProperty, value);
    }

    /// <summary>Stops painting while nobody can see the scene. It picks up where it left off.</summary>
    public bool IsPaused
    {
        get => (bool)GetValue(IsPausedProperty);
        set => SetValue(IsPausedProperty, value);
    }

    /// <summary>A prepared JPEG to show in place of the painted sky. Null shows the painted scene.</summary>
    public byte[]? Photo
    {
        get => (byte[]?)GetValue(PhotoProperty);
        set => SetValue(PhotoProperty, value);
    }

    public bool ShowForeground
    {
        get => (bool)GetValue(ShowForegroundProperty);
        set => SetValue(ShowForegroundProperty, value);
    }

    public static SceneCanvas CreateConfetti() =>
        new(isTransparent: true) { IsHitTestVisible = false, SceneId = "confetti" };

    private void Start()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _clock.Restart();
        _lastFrame = TimeSpan.Zero;
        CompositionTarget.Rendering += OnRendering;
    }

    private void Stop()
    {
        _isRunning = false;
        _clock.Stop();
        CompositionTarget.Rendering -= OnRendering;
    }

    // Left running on a TV all month, so it's throttled well below the display's refresh rate.
    private void OnRendering(object? sender, object e)
    {
        var now = _clock.Elapsed;
        if (IsPaused)
        {
            _lastFrame = now;
            return;
        }

        var budget = TimeSpan.FromSeconds(1 / (CalmMotion ? CalmFramesPerSecond : FramesPerSecond));
        var delta = now - _lastFrame;
        if (delta < budget)
        {
            return;
        }

        _lastFrame = now;
        _sceneTime += (float)delta.TotalSeconds * (CalmMotion ? CalmTimeScale : 1f);
        _invalidate();
    }

    private void Paint(SKCanvas canvas, double pixelWidth, double pixelHeight)
    {
        if (ActualWidth > 0)
        {
            _pixelsPerDip = (float)(pixelWidth / ActualWidth);
        }

        _scene?.Render(canvas, (int)pixelWidth, (int)pixelHeight, _sceneTime);
        canvas.Flush();
    }

    private void OnTapped(object sender, TappedRoutedEventArgs e)
    {
        var point = e.GetPosition(this);
        _scene?.Tap(
            (float)point.X * _pixelsPerDip,
            (float)point.Y * _pixelsPerDip,
            (int)(ActualWidth * _pixelsPerDip),
            (int)(ActualHeight * _pixelsPerDip),
            _sceneTime);
    }

    private void ReplaceScene()
    {
        // Not disposed here: a frame may still be painting it on another thread. The GC gets its images.
        _scene = SceneId == "confetti" ? new ConfettiScene() : Scene.Create(SceneId);
        ApplySnow();
        ApplyCalmMotion();
        ApplyPhoto();
        _invalidate();
    }

    private void DecodePhoto()
    {
        _photoImage = Photo is { } bytes ? SKImage.FromEncodedData(bytes) : null;
        ApplyPhoto();
    }

    private void ApplyCalmMotion()
    {
        if (_scene is not null)
        {
            _scene.CalmMotion = CalmMotion;
        }
    }

    private void ApplyPhoto() => _scene?.SetPhoto(_photoImage, ShowForeground);

    private void ApplySnow()
    {
        if (_scene is not null)
        {
            _scene.Snow = Snow switch
            {
                SnowLevel.Off => SnowAmount.None,
                SnowLevel.Blizzard => SnowAmount.Blizzard,
                _ => SnowAmount.Gentle,
            };
        }
    }

#if !WINAPPSDK
    private sealed partial class SceneElement(SceneCanvas owner) : SKCanvasElement
    {
        // The canvas works in DIPs. Scenes paint real pixels, or their cached backdrop goes blurry on a 4K TV.
        protected override void RenderOverride(SKCanvas canvas, Windows.Foundation.Size area)
        {
            var scale = (float)(XamlRoot?.RasterizationScale ?? 1);

            canvas.Save();
            canvas.Scale(1 / scale);
            owner.Paint(canvas, area.Width * scale, area.Height * scale);
            canvas.Restore();
        }
    }
#endif
}
