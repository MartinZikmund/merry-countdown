using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class RoofDrawingTests
{
    [DataRow(1180, 860)]
    [DataRow(390, 844)]
    [DataRow(360, 220)]
    [TestMethod]
    public void Aurora_CabinRoof_StaysWhiteAboveTheLitWindows(int width, int height)
    {
        using var scene = Scene.Create("aurora");
        scene.Snow = SnowAmount.None;
        var cabin = scene.VisibleElements(width, height, 0).First().Area;

        // The walls are 0.55 of the cabin's width tall, and its tap area is 1.28 of it wide.
        var size = cabin.Width / 1.28f;
        var eaves = cabin.Bottom - (size * 0.55f);
        using var frame = Drawing.Render(scene, width, height, 2);

        // Just above the eaves, over each window, where the glow used to reach.
        foreach (var across in new[] { -0.195f, 0.195f })
        {
            var snow = frame.GetPixel((int)(cabin.MidX + (size * across)), (int)(eaves - (size * 0.03f)));

            snow.Should().Be(Palette.Snow);
        }
    }

    [TestMethod]
    public void House_WindowGlow_StaysUnderTheRoof()
    {
        using SKBitmap bitmap = new(400, 400);
        using SKCanvas canvas = new(bitmap);
        SKRect window = SKRect.Create(180, 190, 40, 46);

        Painter.House(canvas, 200, 300, 200, 130, Palette.WoodDark, Palette.Snow, [window], Palette.Candle);

        bitmap.GetPixel(200, 160).Should().Be(Palette.Snow, "the roof is snow, not glass");
        bitmap.GetPixel(200, 180).Should().NotBe(Palette.WoodDark, "the wall right above the window is lit");
    }
}
