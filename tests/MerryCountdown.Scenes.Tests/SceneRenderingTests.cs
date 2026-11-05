using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class SceneRenderingTests
{
    public static IEnumerable<object[]> Ids => Scene.AllIds.Select(id => new object[] { id });

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Render_PaintsAFullFrameAtEveryScreenShape(string id)
    {
        foreach (var (width, height) in new[] { (1920, 1080), (390, 844), (1024, 1024) })
        {
            using var bitmap = RenderFrame(id, width, height, time: 3);

            ColorCount(bitmap).Should().BeGreaterThan(200, $"{id} at {width}x{height} should be a real picture");
            bitmap.GetPixel(width / 2, 2).Alpha.Should().Be(255);
        }
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Render_ChangesOverTime(string id)
    {
        using var early = RenderFrame(id, 480, 480, time: 1);
        using var late = RenderFrame(id, 480, 480, time: 4);

        Difference(early, late).Should().BeGreaterThan(50, $"{id} should be alive");
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Render_SameTime_SameFrame(string id)
    {
        using var one = RenderFrame(id, 300, 300, time: 2);
        using var two = RenderFrame(id, 300, 300, time: 2);

        Difference(one, two).Should().Be(0);
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Render_ZeroSize_DoesNotThrow(string id)
    {
        using var scene = Scene.Create(id);
        using SKBitmap bitmap = new(1, 1);
        using SKCanvas canvas = new(bitmap);

        scene.Render(canvas, 0, 0, 1);
    }

    [TestMethod]
    public void Tap_ThrowsSparklesIntoTheNextFrame()
    {
        using var scene = Scene.Create("village");
        using var before = Render(scene, 300, 300, 2);

        scene.Tap(150, 150, 300, 300, 2);
        using var after = Render(scene, 300, 300, 2.1f);

        Difference(before, after).Should().BeGreaterThan(100);
    }

    [TestMethod]
    public void Globe_Shaken_LooksDifferentFromSettled()
    {
        using var calm = RenderFrame("globe", 400, 400, 2);
        using var scene = Scene.Create("globe");
        scene.Tap(200, 200, 400, 400, 1.9f);
        using var shaken = Render(scene, 400, 400, 2);

        Difference(calm, shaken).Should().BeGreaterThan(200);
    }

    [TestMethod]
    public void Confetti_LeavesTheBackgroundSeeThrough()
    {
        using ConfettiScene scene = new();
        using SKBitmap bitmap = new(new SKImageInfo(300, 300, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        canvas.Clear(SKColors.Transparent);

        scene.Render(canvas, 300, 300, 2);

        Enumerable.Range(0, 300).Count(x => bitmap.GetPixel(x, 150).Alpha == 0).Should().BeGreaterThan(200);
        ColorCount(bitmap).Should().BeGreaterThan(2);
    }

    [TestMethod]
    public void Create_UnknownId_FallsBackToTheVillage()
    {
        using var scene = Scene.Create("nope");

        scene.Id.Should().Be("village");
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Preview_WritesAPngForAHumanToLookAt(string id)
    {
        var directory = Environment.GetEnvironmentVariable("MERRY_SCENE_PREVIEW_DIR");
        if (string.IsNullOrEmpty(directory))
        {
            return;
        }

        Directory.CreateDirectory(directory);
        foreach (var (name, width, height) in new[] { ("tv", 1280, 720), ("phone", 390, 844) })
        {
            using var bitmap = RenderFrame(id, width, height, time: 7);
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);
            File.WriteAllBytes(Path.Combine(directory, $"{id}-{name}.png"), data.ToArray());
        }
    }

    private static SKBitmap RenderFrame(string id, int width, int height, float time)
    {
        using var scene = Scene.Create(id);
        return Render(scene, width, height, time);
    }

    private static SKBitmap Render(Scene scene, int width, int height, float time)
    {
        SKBitmap bitmap = new(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        scene.Render(canvas, width, height, time);
        canvas.Flush();
        return bitmap;
    }

    private static int ColorCount(SKBitmap bitmap)
    {
        HashSet<SKColor> colors = [];
        for (var y = 0; y < bitmap.Height; y += 7)
        {
            for (var x = 0; x < bitmap.Width; x += 7)
            {
                colors.Add(bitmap.GetPixel(x, y));
            }
        }

        return colors.Count;
    }

    private static int Difference(SKBitmap one, SKBitmap two)
    {
        var count = 0;
        for (var y = 0; y < one.Height; y += 2)
        {
            for (var x = 0; x < one.Width; x += 2)
            {
                if (one.GetPixel(x, y) != two.GetPixel(x, y))
                {
                    count++;
                }
            }
        }

        return count;
    }
}
