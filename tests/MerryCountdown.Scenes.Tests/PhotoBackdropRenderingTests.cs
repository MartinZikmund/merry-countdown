using FluentAssertions;
using SkiaSharp;

namespace MerryCountdown.Scenes.Tests;

[TestClass]
public class PhotoBackdropRenderingTests
{
    private static readonly SKColor _photoColor = new(0xE0, 0x20, 0xE0);

    public static IEnumerable<object[]> Ids => Scene.AllIds.Select(id => new object[] { id });

    private static SKImage SolidPhoto(int width = 400, int height = 300)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.Clear(_photoColor);
        return surface.Snapshot();
    }

    private static SKBitmap Render(Scene scene, float time = 3, int width = 400, int height = 400)
    {
        SKBitmap bitmap = new(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        using SKCanvas canvas = new(bitmap);
        scene.Render(canvas, width, height, time);
        canvas.Flush();
        return bitmap;
    }

    private static double PhotoCoverage(SKBitmap bitmap)
    {
        var hits = 0;
        var total = 0;
        for (var y = 0; y < bitmap.Height; y += 4)
        {
            for (var x = 0; x < bitmap.Width; x += 4)
            {
                var c = bitmap.GetPixel(x, y);
                total++;
                if (c.Red > c.Green * 2 && c.Blue > c.Green * 2)
                {
                    hits++;
                }
            }
        }

        return hits / (double)total;
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Photo_WithoutForeground_FillsTheWholeScreen(string id)
    {
        using var scene = Scene.Create(id);
        scene.Snow = SnowAmount.None;
        using var photo = SolidPhoto();
        scene.SetPhoto(photo, showForeground: false);

        using var bitmap = Render(scene, time: 10);

        PhotoCoverage(bitmap).Should().BeGreaterThan(0.97, $"{id} should show only the photo");
    }

    [TestMethod]
    public void Photo_WideOrTall_CoversEveryCorner()
    {
        foreach (var (w, h) in new[] { (800, 100), (100, 800) })
        {
            using var scene = Scene.Create("village");
            scene.Snow = SnowAmount.None;
            using var photo = SolidPhoto(w, h);
            scene.SetPhoto(photo, showForeground: false);

            using var bitmap = Render(scene, time: 10);

            PhotoCoverage(bitmap).Should().BeGreaterThan(0.97);
        }
    }

    [TestMethod]
    public void Photo_HasShadingAtTheTopSoTheCountdownStaysReadable()
    {
        using var scene = Scene.Create("village");
        scene.Snow = SnowAmount.None;
        using var photo = SolidPhoto();
        scene.SetPhoto(photo, showForeground: false);

        using var bitmap = Render(scene, time: 10);

        bitmap.GetPixel(200, 2).Red.Should().BeLessThan(bitmap.GetPixel(200, 390).Red);
    }

    [DynamicData(nameof(Ids))]
    [TestMethod]
    public void Photo_WithForeground_KeepsThePhotoAndTheScene(string id)
    {
        using var scene = Scene.Create(id);
        scene.Snow = SnowAmount.None;
        using var photo = SolidPhoto();
        scene.SetPhoto(photo, showForeground: true);

        using var bitmap = Render(scene, time: 10);

        // Every scene keeps a piece of the photo, and something of its own in front of it.
        PhotoCoverage(bitmap).Should().BeInRange(0.1, 0.95, id);
    }

    [DataRow("fireplace")]
    [DataRow("globe")]
    [TestMethod]
    public void Photo_WithoutForeground_StillSnows(string id)
    {
        using var photo = SolidPhoto();
        using var calmScene = Scene.Create(id);
        calmScene.Snow = SnowAmount.None;
        calmScene.SetPhoto(photo, showForeground: false);
        using var snowyScene = Scene.Create(id);
        snowyScene.Snow = SnowAmount.Blizzard;
        snowyScene.SetPhoto(photo, showForeground: false);

        using var calm = Render(calmScene, time: 10);
        using var snowy = Render(snowyScene, time: 10);

        var changed = 0;
        for (var y = 0; y < calm.Height; y++)
        {
            for (var x = 0; x < calm.Width; x++)
            {
                if (calm.GetPixel(x, y) != snowy.GetPixel(x, y))
                {
                    changed++;
                }
            }
        }

        changed.Should().BeGreaterThan(200);
    }

    [TestMethod]
    public void RemovingThePhoto_BringsThePaintedSceneBack()
    {
        using var scene = Scene.Create("village");
        scene.Snow = SnowAmount.None;
        using var photo = SolidPhoto();
        scene.SetPhoto(photo, showForeground: false);
        using var withPhoto = Render(scene, time: 10);

        scene.SetPhoto(null, showForeground: true);
        using (Render(scene, time: 20))
        {
        }

        using var without = Render(scene, time: 25);

        PhotoCoverage(without).Should().BeLessThan(0.01);
    }

    [TestMethod]
    public void ChangingPhoto_CrossfadesRatherThanCutting()
    {
        using var scene = Scene.Create("village");
        scene.Snow = SnowAmount.None;
        using var first = SolidPhoto();
        scene.SetPhoto(first, showForeground: false);
        using (Render(scene, time: 10))
        {
        }

        using var surface = SKSurface.Create(new SKImageInfo(50, 50));
        surface.Canvas.Clear(new SKColor(0x20, 0xE0, 0x20));
        using var second = surface.Snapshot();
        scene.SetPhoto(second, showForeground: false);

        using var start = Render(scene, time: 11);
        using var middle = Render(scene, time: 11.5f);
        using var end = Render(scene, time: 14);

        var startGreen = start.GetPixel(200, 390).Green;
        var middleGreen = middle.GetPixel(200, 390).Green;
        var endGreen = end.GetPixel(200, 390).Green;
        startGreen.Should().BeLessThan(middleGreen);
        middleGreen.Should().BeLessThan(endGreen);
    }

    private static byte[] Jpeg(int width, int height)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.Clear(SKColors.SteelBlue);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    // What phones do: pixels stored landscape, plus an EXIF note saying "rotate me".
    private static byte[] WithExifOrientation(byte[] jpeg, ushort orientation)
    {
        byte[] exif =
        [
            0xFF, 0xE1, 0x00, 0x22,
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0,
            (byte)'I', (byte)'I', 0x2A, 0, 8, 0, 0, 0,
            1, 0,
            0x12, 0x01, 3, 0, 1, 0, 0, 0, (byte)orientation, 0, 0, 0,
            0, 0, 0, 0,
        ];
        return [.. jpeg[..2], .. exif, .. jpeg[2..]];
    }

    private static SKBitmap Decode(byte[] bytes) => SKBitmap.Decode(bytes);

    [TestMethod]
    public void Normalize_HugePhoto_IsShrunkKeepingItsShape()
    {
        var result = PhotoImporter.Normalize(Jpeg(4000, 2000));

        using var bitmap = Decode(result!);
        bitmap.Width.Should().Be(PhotoImporter.MaxSide);
        bitmap.Height.Should().Be(PhotoImporter.MaxSide / 2);
    }

    [TestMethod]
    public void Normalize_SmallPhoto_KeepsItsSize()
    {
        using var bitmap = Decode(PhotoImporter.Normalize(Jpeg(640, 480))!);

        bitmap.Width.Should().Be(640);
        bitmap.Height.Should().Be(480);
    }

    [DataRow((ushort)6, 200, 400)]
    [DataRow((ushort)8, 200, 400)]
    [DataRow((ushort)3, 400, 200)]
    [DataRow((ushort)1, 400, 200)]
    [TestMethod]
    public void Normalize_PhonePhoto_IsTurnedTheRightWayUp(ushort orientation, int width, int height)
    {
        var input = WithExifOrientation(Jpeg(400, 200), orientation);

        using var bitmap = Decode(PhotoImporter.Normalize(input)!);

        bitmap.Width.Should().Be(width);
        bitmap.Height.Should().Be(height);
    }

    // Red on the left half of the stored pixels: where it ends up shows which way the photo was turned.
    [DataRow((ushort)6, true)]
    [DataRow((ushort)8, false)]
    [TestMethod]
    public void Normalize_PhonePhoto_TurnsTheCorrectWay(ushort orientation, bool redOnTop)
    {
        using var surface = SKSurface.Create(new SKImageInfo(400, 200));
        surface.Canvas.Clear(SKColors.Blue);
        using SKPaint red = new() { Color = SKColors.Red };
        surface.Canvas.DrawRect(0, 0, 200, 200, red);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);

        using var bitmap = Decode(PhotoImporter.Normalize(WithExifOrientation(data.ToArray(), orientation))!);

        var top = bitmap.GetPixel(100, 50);
        (top.Red > top.Blue).Should().Be(redOnTop);
    }

    [TestMethod]
    public void Normalize_NotAPicture_ReturnsNull()
    {
        PhotoImporter.Normalize([1, 2, 3, 4, 5]).Should().BeNull();
    }
}
