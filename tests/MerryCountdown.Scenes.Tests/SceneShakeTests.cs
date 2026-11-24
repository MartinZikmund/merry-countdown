using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class SceneShakeTests
{
    // The middle of the train's lap, so it's on screen at every shape.
    private const float Time = 13;

    private static readonly (int Width, int Height)[] _shapes = [(1920, 1080), (390, 844), (1024, 1024)];

    public static IEnumerable<object[]> Ids => Scene.AllIds.Select(id => new object[] { id });

    private static Scene Create(string id, bool calm = false)
    {
        var scene = Scene.Create(id);
        scene.Snow = SnowAmount.None;
        scene.CalmMotion = calm;
        return scene;
    }

    private static SKBitmap Render(Scene scene, int width, int height, float time)
    {
        SKBitmap bitmap = new(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        scene.Render(canvas, width, height, time);
        canvas.Flush();
        return bitmap;
    }

    private static SKImage Photo()
    {
        using var surface = SKSurface.Create(new SKImageInfo(100, 100));
        surface.Canvas.Clear(SKColors.SteelBlue);
        return surface.Snapshot();
    }

    // A shake arrives as a push every tenth of a second or so, for as long as it lasts.
    private static void ShakeFor(Scene scene, float strength, float from, float seconds)
    {
        for (var t = from; t <= from + seconds; t += 0.1f)
        {
            scene.Shake(strength, t);
        }
    }

    private static long TotalChange(SKBitmap one, SKBitmap two)
    {
        long total = 0;
        for (var y = 0; y < one.Height; y += 2)
        {
            for (var x = 0; x < one.Width; x += 2)
            {
                var (a, b) = (one.GetPixel(x, y), two.GetPixel(x, y));
                total += Math.Abs(a.Red - b.Red) + Math.Abs(a.Green - b.Green) + Math.Abs(a.Blue - b.Blue);
            }
        }

        return total;
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Shake_MakesTheSceneReact_WithoutABurst(string id)
    {
        foreach (var (width, height) in _shapes)
        {
            using var still = Create(id);
            using var shaken = Create(id);
            using var settledFrame = Render(still, width, height, Time + 0.1f);

            shaken.Shake(1, Time);
            using var shakenFrame = Render(shaken, width, height, Time + 0.1f);

            TotalChange(settledFrame, shakenFrame).Should().BeGreaterThan(1000, $"{id} at {width}x{height} should answer a shake");
            shaken.SparkleCount.Should().Be(0);
        }
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Shake_KeepsReactingWhileItLasts(string id)
    {
        using var still = Create(id);
        using var shaken = Create(id);
        ShakeFor(shaken, 1, Time, 3);

        using var settledFrame = Render(still, 800, 800, Time + 3.05f);
        using var shakenFrame = Render(shaken, 800, 800, Time + 3.05f);

        TotalChange(settledFrame, shakenFrame).Should().BeGreaterThan(1000, $"{id} has been shaken for three seconds and still is");
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Shake_SettlesAfterwards(string id)
    {
        using var still = Create(id);
        using var shaken = Create(id);
        ShakeFor(shaken, 1, Time, 2);

        using var settledFrame = Render(still, 800, 800, Time + 30);
        using var shakenFrame = Render(shaken, 800, 800, Time + 30);

        TotalChange(settledFrame, shakenFrame).Should().Be(0, $"{id} should be back to normal long after the shake");
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Shake_Harder_ReactsMore(string id)
    {
        using var still = Create(id);
        using var gentle = Create(id);
        using var wild = Create(id);
        gentle.Shake(0.3f, Time);
        wild.Shake(1, Time);

        using var settledFrame = Render(still, 800, 800, Time + 0.2f);
        using var gentleFrame = Render(gentle, 800, 800, Time + 0.2f);
        using var wildFrame = Render(wild, 800, 800, Time + 0.2f);

        TotalChange(settledFrame, wildFrame).Should().BeGreaterThan(TotalChange(settledFrame, gentleFrame), id);
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void CalmMotion_Shake_StillAnswers_ButGentler(string id)
    {
        foreach (var (width, height) in _shapes)
        {
            using var still = Create(id, calm: true);
            using var calm = Create(id, calm: true);
            using var lively = Create(id);
            using var livelyStill = Create(id);
            calm.Shake(1, Time);
            lively.Shake(1, Time);

            using var calmStillFrame = Render(still, width, height, Time + 0.2f);
            using var calmFrame = Render(calm, width, height, Time + 0.2f);
            using var livelyStillFrame = Render(livelyStill, width, height, Time + 0.2f);
            using var livelyFrame = Render(lively, width, height, Time + 0.2f);

            var calmChange = TotalChange(calmStillFrame, calmFrame);
            calmChange.Should().BeGreaterThan(0, $"{id} at {width}x{height} should still answer");
            calmChange.Should().BeLessThan(TotalChange(livelyStillFrame, livelyFrame), $"{id} at {width}x{height} should be gentler");
        }
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Photo_WithoutForeground_ShakeThrowsUpSnow(string id)
    {
        using var photo = Photo();
        using var still = Create(id);
        using var shaken = Create(id);
        still.SetPhoto(photo, showForeground: false);
        shaken.SetPhoto(photo, showForeground: false);

        shaken.Shake(1, Time);
        using var settledFrame = Render(still, 800, 800, Time + 0.2f);
        using var shakenFrame = Render(shaken, 800, 800, Time + 0.2f);
        using var laterStill = Render(still, 800, 800, Time + 30);
        using var laterShaken = Render(shaken, 800, 800, Time + 30);

        TotalChange(settledFrame, shakenFrame).Should().BeGreaterThan(1000, id);
        TotalChange(laterStill, laterShaken).Should().Be(0, id);
    }
}
