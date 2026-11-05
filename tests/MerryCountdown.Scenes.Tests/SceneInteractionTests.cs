using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class SceneInteractionTests
{
    // The middle of the train's lap, so it's on screen at every shape.
    private const float Time = 13;

    private static readonly (int Width, int Height)[] _shapes = [(1920, 1080), (390, 844), (1024, 1024)];

    public static IEnumerable<object[]> Ids => Scene.AllIds.Select(id => new object[] { id });

    private static Scene Create(string id)
    {
        var scene = Scene.Create(id);
        scene.Snow = SnowAmount.None;
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

    private static int ChangedPixels(SKBitmap one, SKBitmap two, Func<int, int, bool>? where = null)
    {
        var count = 0;
        for (var y = 0; y < one.Height; y += 2)
        {
            for (var x = 0; x < one.Width; x += 2)
            {
                if ((where?.Invoke(x, y) ?? true) && one.GetPixel(x, y) != two.GetPixel(x, y))
                {
                    count++;
                }
            }
        }

        return count;
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

    private static SKPoint PointOutsideEveryElement(Scene scene, int width, int height)
    {
        for (var y = height - 1f; y > 0; y -= height / 40f)
        {
            for (var x = 1f; x < width; x += width / 40f)
            {
                if (scene.ElementAt(x, y, width, height, Time) is null)
                {
                    return new SKPoint(x, y);
                }
            }
        }

        throw new AssertFailedException($"{scene.Id} has nothing left to throw a burst from at {width}x{height}.");
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Scene_HasAnInteractiveElementAtEveryScreenShape(string id)
    {
        foreach (var (width, height) in _shapes)
        {
            using var scene = Create(id);

            scene.VisibleElements(width, height, Time).Should().NotBeEmpty($"{id} at {width}x{height}");
        }
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Tap_OnAnElement_ReactsWithoutABurst(string id)
    {
        foreach (var (width, height) in _shapes)
        {
            using var counting = Create(id);
            foreach (var element in counting.VisibleElements(width, height, Time).ToList())
            {
                using var scene = Create(id);
                using var still = Render(scene, width, height, Time + 0.3f);

                var reacted = scene.Tap(element.Area.MidX, element.Area.MidY, width, height, Time);
                using var after = Render(scene, width, height, Time + 0.3f);

                reacted.Should().BeTrue($"{id} at {width}x{height}, tapped on {element.Area}");
                scene.SparkleCount.Should().Be(0);
                ChangedPixels(still, after).Should().BeGreaterThan(0, $"tapping {element.Area} in {id} should show");
            }
        }
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Tap_OutsideEveryElement_BurstsWithoutAReaction(string id)
    {
        foreach (var (width, height) in _shapes)
        {
            using var scene = Create(id);
            var point = PointOutsideEveryElement(scene, width, height);
            using var still = Render(scene, width, height, Time + 0.05f);

            var reacted = scene.Tap(point.X, point.Y, width, height, Time);
            using var after = Render(scene, width, height, Time + 0.05f);

            reacted.Should().BeFalse();
            scene.SparkleCount.Should().BeGreaterThan(0);

            // Sparkles have barely left the finger, so anything changed further away would be an element reacting.
            var reach = Math.Min(width, height) * 0.08f;
            ChangedPixels(still, after, (x, y) => MathF.Abs(x - point.X) > reach || MathF.Abs(y - point.Y) > reach)
                .Should().Be(0, $"{id} at {width}x{height}, tapped at {point}");
        }
    }

    [TestMethod]
    public void Globe_TappedOutsideTheGlass_DoesNotShake()
    {
        using var scene = Create("globe");
        using var settled = Render(scene, 400, 400, 2);

        scene.Tap(5, 5, 400, 400, 1.9f);
        using var after = Render(scene, 400, 400, 2);

        scene.SparkleCount.Should().BeGreaterThan(0);

        // The glass is well below this corner, and it's the only thing a shake would change.
        ChangedPixels(settled, after, (x, y) => y > 150).Should().Be(0);
    }

    [TestMethod]
    public void Globe_TappedOnTheGlass_ShakesWithoutABurst()
    {
        using var scene = Create("globe");
        using var settled = Render(scene, 400, 400, 2);

        scene.Tap(200, 225, 400, 400, 1.9f);
        using var after = Render(scene, 400, 400, 2);

        scene.SparkleCount.Should().Be(0);
        ChangedPixels(settled, after).Should().BeGreaterThan(200);
    }

    [TestMethod]
    public void Train_HitAreaFollowsTheTrain()
    {
        using var scene = Create("train");

        var early = scene.VisibleElements(1000, 1000, 6).Single().Area;
        var late = scene.VisibleElements(1000, 1000, 16).Single().Area;

        late.MidX.Should().BeGreaterThan(early.MidX);
        scene.ElementAt(early.MidX, early.MidY, 1000, 1000, 6).Should().NotBeNull();
        scene.ElementAt(early.Left - 100, early.MidY, 1000, 1000, 6).Should().BeNull();
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Tap_NearAnElement_StillCounts(string id)
    {
        using var scene = Create(id);
        var element = scene.VisibleElements(1000, 1000, Time).First();
        var justOutside = element.IsRound ? element.Area.MidX + (element.Area.Width / 2) + 2 : element.Area.Right + 2;

        scene.ElementAt(justOutside, element.Area.MidY, 1000, 1000, Time).Should().NotBeNull("small fingers aren't precise");
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void CalmMotion_StillReacts_ButLessThanUsual(string id)
    {
        foreach (var (width, height) in _shapes)
        {
            using var counting = Create(id);
            foreach (var element in counting.VisibleElements(width, height, Time).ToList())
            {
                using var lively = Create(id);
                using var calm = Create(id);
                calm.CalmMotion = true;
                using var still = Render(lively, width, height, Time + 0.3f);

                calm.Tap(element.Area.MidX, element.Area.MidY, width, height, Time);
                lively.Tap(element.Area.MidX, element.Area.MidY, width, height, Time);
                using var calmFrame = Render(calm, width, height, Time + 0.3f);
                using var livelyFrame = Render(lively, width, height, Time + 0.3f);

                ChangedPixels(still, calmFrame).Should().BeGreaterThan(0, $"{id} should still answer a tap on {element.Area}");
                TotalChange(still, calmFrame).Should().BeLessThan(TotalChange(still, livelyFrame), $"{id} on {element.Area} should be gentler");
            }
        }
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Photo_WithoutForeground_LeavesNothingToTapButTheBurst(string id)
    {
        using var scene = Create(id);
        var element = scene.VisibleElements(1000, 1000, Time).First();
        using var photo = Photo();
        scene.SetPhoto(photo, showForeground: false);

        var reacted = scene.Tap(element.Area.MidX, element.Area.MidY, 1000, 1000, Time);

        reacted.Should().BeFalse();
        scene.SparkleCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    public void Photo_WithForeground_KeepsTheLandButNotTheSky()
    {
        using var scene = Create("aurora");
        using var photo = Photo();
        var elements = scene.VisibleElements(1000, 1000, Time).Count();
        scene.SetPhoto(photo, showForeground: true);

        var visible = scene.VisibleElements(1000, 1000, Time).ToList();

        visible.Should().HaveCount(elements - 1);
        visible.Should().OnlyContain(e => e.Area.MidY > 500);
    }

    [TestMethod]
    public void Reaction_DiesAway()
    {
        using var scene = Create("fireplace");
        var element = scene.VisibleElements(1000, 1000, Time).First();
        scene.Tap(element.Area.MidX, element.Area.MidY, 1000, 1000, Time);

        using var later = Render(scene, 1000, 1000, Time + 30);
        using var untouched = Create("fireplace");
        using var never = Render(untouched, 1000, 1000, Time + 30);

        ChangedPixels(later, never).Should().Be(0);
    }
}
