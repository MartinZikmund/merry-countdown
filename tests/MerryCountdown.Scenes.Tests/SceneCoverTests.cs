using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class SceneCoverTests
{
    // The mini window: a sign across the top 45% of a small landscape frame.
    private const int Width = 630;
    private const int Height = 399;
    private const float Cover = 0.45f;

    [DataRow("globe")]
    [DataRow("fireplace")]
    [TestMethod]
    public void CoveredTop_Centrepiece_StaysBelowTheSign(string id)
    {
        using var scene = Scene.Create(id);
        scene.CoveredTop = Cover;

        var highest = scene.VisibleElements(Width, Height, 1).Min(e => e.Area.Top);

        highest.Should().BeGreaterThan(Height * Cover);
    }

    [DataRow("globe")]
    [DataRow("fireplace")]
    [TestMethod]
    public void CoveredTop_WithRoomToSpare_ChangesNothing(string id)
    {
        using var open = Render(id, 1920, 1080, 0);
        using var covered = Render(id, 1920, 1080, 0.2f);

        SolidChanges(open, covered, toRow: 1080, tolerance: 0).Should().Be(0);
    }

    [TestMethod]
    public void CoveredTop_Changed_RepaintsTheStillParts()
    {
        using var scene = Scene.Create("fireplace");
        using var before = Render(scene, Width, Height);

        scene.CoveredTop = Cover;
        using var after = Render(scene, Width, Height);

        SolidChanges(before, after, toRow: Height).Should().BeGreaterThan(500);
    }

    [TestMethod]
    public void CoveredTop_NearlyEverything_StillLeavesAScene()
    {
        using var scene = Scene.Create("globe");

        scene.CoveredTop = 0.95f;

        scene.CoveredTop.Should().BeLessThanOrEqualTo(0.6f);
    }

    private static SKBitmap Render(string id, int width, int height, float cover)
    {
        using var scene = Scene.Create(id);
        scene.CoveredTop = cover;
        return Render(scene, width, height);
    }

    private static SKBitmap Render(Scene scene, int width, int height)
    {
        SKBitmap bitmap = new(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        scene.Render(canvas, width, height, 1);
        canvas.Flush();
        return bitmap;
    }

    private static int SolidChanges(SKBitmap one, SKBitmap two, int toRow, int tolerance = 40)
    {
        var count = 0;
        for (var y = 0; y < toRow; y += 2)
        {
            for (var x = 0; x < one.Width; x += 2)
            {
                var (a, b) = (one.GetPixel(x, y), two.GetPixel(x, y));
                if (Math.Abs(a.Red - b.Red) + Math.Abs(a.Green - b.Green) + Math.Abs(a.Blue - b.Blue) > tolerance)
                {
                    count++;
                }
            }
        }

        return count;
    }
}
