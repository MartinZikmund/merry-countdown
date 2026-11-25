using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class VillageDrawingTests
{
    private const float X = 300;
    private const float Ground = 560;
    private const float Tall = 500;

    [TestMethod]
    public void TreeLight_Every_HangsOnTheTree()
    {
        using var pine = PaintPine();

        for (var i = 0; i < 26; i++)
        {
            var light = VillageScene.TreeLight(i, X, Ground, Tall);

            pine.GetPixel((int)light.X, (int)light.Y).Alpha.Should().Be(255, $"light {i} at {light} belongs on a branch");
        }
    }

    [TestMethod]
    public void TreeLight_Lights_ReachEveryTierOfTheTree()
    {
        var depths = Enumerable.Range(0, 26).Select(i => (VillageScene.TreeLight(i, X, Ground, Tall).Y - (Ground - Tall)) / Tall).ToList();

        // The tree's three tiers end 0.46, 0.72 and 0.98 of the way down.
        depths.Count(d => d < 0.46f).Should().BeGreaterThan(4);
        depths.Count(d => d is >= 0.46f and < 0.72f).Should().BeGreaterThan(4);
        depths.Count(d => d >= 0.72f).Should().BeGreaterThan(4, "the bottom tier is the biggest and used to be bare");
    }

    [TestMethod]
    public void TreeLight_Lights_SpreadOverBothSides()
    {
        var lights = Enumerable.Range(0, 26).Select(i => VillageScene.TreeLight(i, X, Ground, Tall)).ToList();

        lights.Count(l => l.X < X - 5).Should().BeGreaterThan(5);
        lights.Count(l => l.X > X + 5).Should().BeGreaterThan(5);
    }

    [TestMethod]
    public void PineHalfWidth_AtEveryDepth_MatchesThePaintedTree()
    {
        using var pine = PaintPine();

        for (var depth = 0.05f; depth < 0.97f; depth += 0.03f)
        {
            var half = Painter.PineHalfWidth(depth) * Tall;
            var y = (int)(Ground - Tall + (depth * Tall));

            pine.GetPixel((int)(X + half - 3), y).Alpha.Should().Be(255, $"that is inside the tree at depth {depth}");

            // Higher up the snow caps and the sagging hems stick out past the green, so only the bottom tier is exact.
            if (depth > 0.75f)
            {
                pine.GetPixel((int)(X + half + 3), y).Alpha.Should().Be(0, $"that is outside the tree at depth {depth}");
            }
        }
    }

    private static SKBitmap PaintPine()
    {
        SKBitmap bitmap = new(600, 600);
        using SKCanvas canvas = new(bitmap);
        Painter.Pine(canvas, X, Ground, Tall, Palette.Pine, Palette.Snow);
        return bitmap;
    }
}
