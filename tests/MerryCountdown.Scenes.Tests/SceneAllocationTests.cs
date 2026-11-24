using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class SceneAllocationTests
{
    // Every scene sits around 3 to 11 KB a frame. Far more than that means something is built per pixel column or per flake.
    private const long MostBytesPerFrame = 16 * 1024;

    public static IEnumerable<object[]> Ids => [.. Scene.AllIds.Select(id => new object[] { id }), ["confetti"]];

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Render_SteadyFrame_AllocatesLittle(string id)
    {
        const int Frames = 60;
        using Scene scene = id == "confetti" ? new ConfettiScene() : Scene.Create(id);
        using SKBitmap bitmap = new(new SKImageInfo(1920, 1080, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        scene.Render(canvas, 1920, 1080, 0);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 1; i <= Frames; i++)
        {
            scene.Render(canvas, 1920, 1080, i / 30f);
        }

        var perFrame = (GC.GetAllocatedBytesForCurrentThread() - before) / Frames;

        perFrame.Should().BeLessThan(MostBytesPerFrame, $"{id} runs 30 times a second all December");
    }
}
