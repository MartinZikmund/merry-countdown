using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class GlobeDrawingTests
{
    [DataRow(1180, 860, false)]
    [DataRow(390, 844, false)]
    [DataRow(1180, 860, true)]
    [TestMethod]
    public void Globe_FallingSnow_NeverShowsOverTheBase(int width, int height, bool overPhoto)
    {
        using var scene = Scene.Create("globe");
        using var photo = Drawing.SolidPhoto();
        if (overPhoto)
        {
            scene.SetPhoto(photo, showForeground: true);
        }

        var glass = scene.VisibleElements(width, height, 0).Single().Area;
        var (cx, cy, radius) = (glass.MidX, glass.MidY, glass.Width / 2);

        // The front of the base, right under the rim: the bottom of the ball used to show here.
        SKRectI front = new((int)(cx - (radius * 0.35f)), (int)(cy + (radius * 0.92f)), (int)(cx + (radius * 0.35f)), (int)(cy + radius));

        using var settled = Drawing.Render(scene, width, height, 0);
        for (var time = 1.5f; time < 40; time += 1.5f)
        {
            using var later = Drawing.Render(scene, width, height, time);

            Drawing.Changes(settled, later, front).Should().Be(0, $"the base is solid, at {time} s");
        }
    }

    [TestMethod]
    public void Globe_Shaken_KeepsTheSnowOffTheBase()
    {
        const int Width = 1180;
        const int Height = 860;
        using var scene = Scene.Create("globe");
        var glass = scene.VisibleElements(Width, Height, 0).Single().Area;
        var (cx, cy, radius) = (glass.MidX, glass.MidY, glass.Width / 2);
        SKRectI front = new((int)(cx - (radius * 0.35f)), (int)(cy + (radius * 0.92f)), (int)(cx + (radius * 0.35f)), (int)(cy + radius));
        using var settled = Drawing.Render(scene, Width, Height, 0);

        scene.Shake(1, 1);
        using var shaken = Drawing.Render(scene, Width, Height, 1.3f);

        Drawing.Changes(settled, shaken, front).Should().Be(0);
    }
}
