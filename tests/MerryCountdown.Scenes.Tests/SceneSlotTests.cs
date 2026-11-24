using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class SceneSlotTests
{
    private static SKBitmap NewBitmap() => new(new SKImageInfo(64, 64, SKImageInfo.PlatformColorType, SKAlphaType.Premul));

    [TestMethod]
    public void Replace_DisposesThePreviousScene()
    {
        using SceneSlot slot = new();
        var first = Scene.Create("village");
        slot.Replace(first);

        slot.Replace(Scene.Create("globe"));

        first.IsDisposed.Should().BeTrue();
        slot.Scene!.IsDisposed.Should().BeFalse();
    }

    [TestMethod]
    public void Replace_SameScene_KeepsIt()
    {
        using SceneSlot slot = new();
        var scene = Scene.Create("village");
        slot.Replace(scene);

        slot.Replace(scene);

        scene.IsDisposed.Should().BeFalse();
    }

    [TestMethod]
    public void Dispose_DisposesTheCurrentScene()
    {
        SceneSlot slot = new();
        var scene = Scene.Create("village");
        slot.Replace(scene);

        slot.Dispose();

        scene.IsDisposed.Should().BeTrue();
        slot.Scene.Should().BeNull();
    }

    [TestMethod]
    public void Render_Empty_DrawsNothing()
    {
        using SceneSlot slot = new();
        using var bitmap = NewBitmap();
        using SKCanvas canvas = new(bitmap);

        slot.Render(canvas, 64, 64, 1);

        bitmap.GetPixel(32, 32).Alpha.Should().Be(0);
    }

    // Some heads paint on a thread of their own, so the scene being swapped out may be mid-frame.
    [TestMethod]
    public async Task Replace_WhileAFrameIsPainting_DisposesOnlyAfterTheFrame()
    {
        using SceneSlot slot = new();
        using BlockingScene painting = new();
        slot.Replace(painting);
        using var bitmap = NewBitmap();

        var frame = Task.Run(() =>
        {
            using SKCanvas canvas = new(bitmap);
            slot.Render(canvas, 64, 64, 1);
        });
        painting.Started.Wait(TimeSpan.FromSeconds(5)).Should().BeTrue();

        var replace = Task.Run(() => slot.Replace(Scene.Create("globe")));
        await Task.Delay(100);
        painting.IsDisposed.Should().BeFalse("the frame is still painting it");

        painting.Finish.Set();
        await Task.WhenAll(frame, replace);

        painting.IsDisposed.Should().BeTrue();
    }

    private sealed class BlockingScene : Scene
    {
        public ManualResetEventSlim Started { get; } = new();

        public ManualResetEventSlim Finish { get; } = new();

        public override string Id => "blocking";

        protected override void PaintSky(SKCanvas canvas, int width, int height) => canvas.Clear(SKColors.Navy);

        protected override void PaintSkyLife(SKCanvas canvas, int width, int height, float time)
        {
            Started.Set();
            Finish.Wait(TimeSpan.FromSeconds(10));
        }
    }
}
