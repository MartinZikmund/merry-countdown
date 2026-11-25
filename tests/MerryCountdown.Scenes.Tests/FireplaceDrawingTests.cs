using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class FireplaceDrawingTests
{
    [DataRow(390, 844)]
    [DataRow(320, 568)]
    [DataRow(1180, 860)]
    [DataRow(360, 220)]
    [TestMethod]
    public void Fireplace_AtEveryShape_KeepsEveryTappableThingOnScreen(int width, int height)
    {
        using var scene = Scene.Create("fireplace");

        var areas = scene.VisibleElements(width, height, 0).Select(e => e.Area).ToList();

        areas.Should().HaveCount(7, "the flames, three stockings and three presents");
        areas.Should().OnlyContain(a => a.Left >= 0 && a.Right <= width);
    }

    [DataRow(1180, 860)]
    [DataRow(390, 844)]
    [TestMethod]
    public void Fireplace_BricksAndLogs_StayOffTheFloor(int width, int height)
    {
        using var scene = Scene.Create("fireplace");
        var flames = scene.VisibleElements(width, height, 0).First().Area;

        // The flames' area is as wide as the firebox, which is 0.92 of a hearth.
        var hearth = flames.Width / 0.92f;
        var y = (int)flames.Bottom + 3;
        using var frame = Drawing.Render(scene, width, height, 2);

        // Under the wall and the firebox, between the presents: bare floor, so no line or log end may break it.
        for (var x = (int)(flames.MidX - (hearth * 0.6f)); x < flames.MidX + (hearth * 0.6f); x++)
        {
            Drawing.Distance(frame.GetPixel(x, y), frame.GetPixel(x + 1, y)).Should().BeLessThan(12, $"the floor is smooth at {x}");
        }
    }
}
