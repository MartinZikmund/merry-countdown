using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

/// <summary>Frames and pixel checks shared by the tests that look at how a scene is drawn.</summary>
internal static class Drawing
{
    public static SKBitmap Render(Scene scene, int width, int height, float time)
    {
        SKBitmap bitmap = new(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        scene.Render(canvas, width, height, time);
        canvas.Flush();
        return bitmap;
    }

    public static SKImage SolidPhoto()
    {
        using var surface = SKSurface.Create(new SKImageInfo(400, 300));
        surface.Canvas.Clear(new SKColor(0xE0, 0xE8, 0xF0));
        return surface.Snapshot();
    }

    /// <summary>How many pixels of the area differ between two frames.</summary>
    public static int Changes(SKBitmap one, SKBitmap two, SKRectI area, int tolerance = 0)
    {
        var count = 0;
        for (var y = area.Top; y < area.Bottom; y++)
        {
            for (var x = area.Left; x < area.Right; x++)
            {
                if (Distance(one.GetPixel(x, y), two.GetPixel(x, y)) > tolerance)
                {
                    count++;
                }
            }
        }

        return count;
    }

    public static int Distance(SKColor a, SKColor b) =>
        Math.Abs(a.Red - b.Red) + Math.Abs(a.Green - b.Green) + Math.Abs(a.Blue - b.Blue) + Math.Abs(a.Alpha - b.Alpha);
}
