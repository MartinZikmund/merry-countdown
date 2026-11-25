using System.Diagnostics;
using System.Globalization;
using MerryCountdown.Scenes;
using SkiaSharp;

// Usage:
//   SceneBench frames [frames-per-run] [out.csv]
//   SceneBench memory <scenario> [out.csv]    scenarios: steady, swap, swap-dispose, swap-slot, resize, photos, photos-dispose, taps
//   SceneBench render <subject> <dir> [w h]       PNGs of a few frames, to check a speed-up didn't change the picture
//                                             subjects: a scene id, confetti, photo, or photo+<scene id>
//   SceneBench compare <dirA> <dirB>          how far apart two sets of those PNGs are
// Run a Release build, one memory scenario per process, so one doesn't inherit another's heap.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

var mode = args.ElementAtOrDefault(0) ?? "frames";
return mode switch
{
    "frames" => Frames.Run(int.Parse(args.ElementAtOrDefault(1) ?? "2000"), args.ElementAtOrDefault(2)),
    "memory" => Memory.Run(args.ElementAtOrDefault(1) ?? "steady", args.ElementAtOrDefault(2)),
    "render" => Pictures.Render(args[1], args[2], int.Parse(args.ElementAtOrDefault(3) ?? "1920"), int.Parse(args.ElementAtOrDefault(4) ?? "1080")),
    "compare" => Pictures.Compare(args[1], args[2]),
    _ => Usage(),
};

static int Usage()
{
    Console.Error.WriteLine("SceneBench frames [count] [out.csv] | SceneBench memory <scenario> [out.csv]");
    return 1;
}

internal static class Subjects
{
    public const float FrameSeconds = 1 / 30f;
    public const float CalmFrameSeconds = 0.35f / 10f;

    public static readonly string[] All = [.. Scene.AllIds, "confetti", "photo+village", "photo"];

    // "photo+globe" and so on: any scene's foreground in front of the photo.
    private const string PhotoBehind = "photo+";

    private static byte[]? _photoJpeg;

    // A busy 2560x1440 picture, the largest the importer keeps, so the decode costs what a real one does.
    public static byte[] PhotoJpeg(int seed = 0)
    {
        if (seed == 0 && _photoJpeg is not null)
        {
            return _photoJpeg;
        }

        using var surface = SKSurface.Create(new SKImageInfo(PhotoImporter.MaxSide, 1440));
        var canvas = surface.Canvas;
        Random random = new(seed + 7);
        canvas.Clear(new SKColor((byte)random.Next(256), (byte)random.Next(256), (byte)random.Next(256)));
        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < 400; i++)
        {
            paint.Color = new SKColor((uint)random.Next()).WithAlpha(255);
            canvas.DrawCircle(random.Next(PhotoImporter.MaxSide), random.Next(1440), 10 + random.Next(120), paint);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        var bytes = data.ToArray();
        if (seed == 0)
        {
            _photoJpeg = bytes;
        }

        return bytes;
    }

    /// <summary>Builds the subject the way SceneCanvas does. The photo, if any, belongs to the caller.</summary>
    public static (Scene Scene, SKImage? Photo) Create(string subject, bool calm)
    {
        Scene scene = subject switch
        {
            "confetti" => new ConfettiScene(),
            "photo" => Scene.Create("village"),
            _ when subject.StartsWith(PhotoBehind, StringComparison.Ordinal) => Scene.Create(subject[PhotoBehind.Length..]),
            _ => Scene.Create(subject),
        };
        scene.CalmMotion = calm;

        SKImage? photo = null;
        if (subject.StartsWith("photo", StringComparison.Ordinal))
        {
            photo = SKImage.FromEncodedData(PhotoJpeg());
            scene.SetPhoto(photo, showForeground: subject.StartsWith(PhotoBehind, StringComparison.Ordinal));
        }

        return (scene, photo);
    }
}

internal static class Frames
{
    public static int Run(int count, string? outPath)
    {
        List<string> rows = ["subject,width,height,calm,frames,mean_ms,p50_ms,p99_ms,max_ms,bytes_per_frame,gen0_per_1000_frames"];
        Console.WriteLine($"{"subject",-14} {"size",-10} {"calm",-5} {"mean",7} {"p99",7} {"max",7} {"B/frame",9} {"gen0/1k",8}");

        foreach (var (width, height) in new[] { (1920, 1080), (3840, 2160) })
        {
            using var surface = SKSurface.Create(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
            foreach (var subject in Subjects.All)
            {
                foreach (var calm in new[] { false, true })
                {
                    var row = Measure(surface.Canvas, subject, calm, width, height, count);
                    rows.Add(row);
                }
            }
        }

        if (outPath is not null)
        {
            File.WriteAllLines(outPath, rows);
        }

        return 0;
    }

    private static string Measure(SKCanvas canvas, string subject, bool calm, int width, int height, int count)
    {
        var (scene, photo) = Subjects.Create(subject, calm);
        var step = calm ? Subjects.CalmFrameSeconds : Subjects.FrameSeconds;
        var clear = subject == "confetti";
        var time = 0f;

        void Frame()
        {
            if (clear)
            {
                canvas.Clear(SKColors.Transparent);
            }

            scene.Render(canvas, width, height, time);
            canvas.Flush();
            time += step;
        }

        // Past the first backdrop paint and the photo crossfade, so the numbers are the steady state.
        for (var i = 0; i < 60; i++)
        {
            Frame();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var times = new double[count];
        var gen0 = GC.CollectionCount(0);
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < count; i++)
        {
            var start = Stopwatch.GetTimestamp();
            Frame();
            times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        }

        var bytesPerFrame = (GC.GetAllocatedBytesForCurrentThread() - allocated) / (double)count;
        var gen0Per1000 = (GC.CollectionCount(0) - gen0) * 1000.0 / count;
        scene.Dispose();
        photo?.Dispose();

        Array.Sort(times);
        var mean = times.Average();
        var p50 = times[count / 2];
        var p99 = times[(int)(count * 0.99)];
        var max = times[^1];
        Console.WriteLine($"{subject,-14} {width + "x" + height,-10} {(calm ? "on" : "off"),-5} {mean,7:F2} {p99,7:F2} {max,7:F2} {bytesPerFrame,9:F0} {gen0Per1000,8:F1}");
        return $"{subject},{width},{height},{calm},{count},{mean:F3},{p50:F3},{p99:F3},{max:F3},{bytesPerFrame:F0},{gen0Per1000:F2}";
    }
}

internal static class Pictures
{
    private static readonly float[] _times = [0, 1.3f, 4.7f, 9.1f, 33.3f];

    public static int Render(string subject, string dir, int width, int height)
    {
        Directory.CreateDirectory(dir);
        foreach (var calm in new[] { false, true })
        {
            foreach (var tapped in new[] { false, true })
            {
                foreach (var time in _times)
                {
                    var (scene, photo) = Subjects.Create(subject, calm);
                    using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
                    using (SKCanvas canvas = new(bitmap))
                    {
                        scene.Render(canvas, width, height, time);
                        if (tapped)
                        {
                            // The sky and the middle of the ground: whatever the scene has there reacts.
                            scene.Tap(width / 2f, height * 300f / 1080, width, height, time);
                            scene.Tap(width / 2f, height * 900f / 1080, width, height, time);
                        }

                        canvas.Clear(SKColors.Black);
                        scene.Render(canvas, width, height, time + 0.4f);
                    }

                    using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                    File.WriteAllBytes(Path.Combine(dir, $"{subject}-{width}x{height}-{(calm ? "calm" : "lively")}-{(tapped ? "tapped" : "idle")}-{time:F1}.png"), data.ToArray());
                    scene.Dispose();
                    photo?.Dispose();
                }
            }
        }

        return 0;
    }

    public static int Compare(string dirA, string dirB)
    {
        var worst = 0;
        foreach (var pathA in Directory.GetFiles(dirA, "*.png").Order())
        {
            using var a = SKBitmap.Decode(pathA);
            using var b = SKBitmap.Decode(Path.Combine(dirB, Path.GetFileName(pathA)));
            var (maxDiff, over2, total) = (0, 0, a.Width * a.Height);
            for (var y = 0; y < a.Height; y++)
            {
                for (var x = 0; x < a.Width; x++)
                {
                    var (ca, cb) = (a.GetPixel(x, y), b.GetPixel(x, y));
                    var d = Math.Max(Math.Max(Math.Abs(ca.Red - cb.Red), Math.Abs(ca.Green - cb.Green)), Math.Max(Math.Abs(ca.Blue - cb.Blue), Math.Abs(ca.Alpha - cb.Alpha)));
                    maxDiff = Math.Max(maxDiff, d);
                    over2 += d > 2 ? 1 : 0;
                }
            }

            worst = Math.Max(worst, maxDiff);
            Console.WriteLine($"{Path.GetFileName(pathA),-40} max channel diff {maxDiff,3}, pixels off by more than 2: {over2 * 100.0 / total:F3}%");
        }

        Console.WriteLine($"worst channel diff {worst}");
        return 0;
    }
}

internal static class Memory
{
    private const int Width = 1920;
    private const int Height = 1080;

    public static int Run(string scenario, string? outPath)
    {
        using var surface = SKSurface.Create(new SKImageInfo(Width, Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul));
        Sampler sampler = new(scenario);
        var canvas = surface.Canvas;

        // Warm up Skia, the JIT and the photo so the baseline isn't mostly one-off costs.
        Subjects.PhotoJpeg();
        using (var warm = Scene.Create("village"))
        {
            warm.Render(canvas, Width, Height, 0);
        }

        Sampler.Settle();
        sampler.Sample(0, "baseline");

        switch (scenario)
        {
            case "steady":
                Steady(canvas, sampler);
                break;
            case "swap":
            case "swap-dispose":
                Swap(canvas, sampler, dispose: scenario == "swap-dispose");
                break;
            case "swap-slot":
                SwapThroughSlot(canvas, sampler);
                break;
            case "resize":
                Resize(surface, sampler);
                break;
            case "photos":
            case "photos-dispose":
                Photos(canvas, sampler, dispose: scenario == "photos-dispose");
                break;
            case "taps":
                Taps(canvas, sampler);
                break;
            default:
                Console.Error.WriteLine($"Unknown scenario {scenario}");
                return 1;
        }

        sampler.Sample(-1, "end before GC");
        Sampler.Settle();
        sampler.Sample(-1, "end after full GC");
        sampler.Save(outPath);
        return 0;
    }

    private static void Steady(SKCanvas canvas, Sampler sampler)
    {
        using var scene = Scene.Create("village");
        for (var i = 1; i <= 10_000; i++)
        {
            scene.Render(canvas, Width, Height, i * Subjects.FrameSeconds);
            sampler.Every(i, 500);
        }
    }

    // What SceneCanvas.ReplaceScene does: a new scene, a few frames, the old one dropped.
    private static void Swap(SKCanvas canvas, Sampler sampler, bool dispose)
    {
        Scene? current = null;
        var time = 0f;
        for (var i = 1; i <= 500; i++)
        {
            var next = Scene.Create(Scene.AllIds[i % Scene.AllIds.Count]);
            if (dispose)
            {
                current?.Dispose();
            }

            current = next;
            for (var f = 0; f < 5; f++)
            {
                current.Render(canvas, Width, Height, time += Subjects.FrameSeconds);
            }

            sampler.Every(i, 10);
        }

        current?.Dispose();
    }

    // What SceneCanvas does now: the slot frees the scene it replaces.
    private static void SwapThroughSlot(SKCanvas canvas, Sampler sampler)
    {
        using SceneSlot slot = new();
        var time = 0f;
        for (var i = 1; i <= 500; i++)
        {
            slot.Replace(Scene.Create(Scene.AllIds[i % Scene.AllIds.Count]));
            for (var f = 0; f < 5; f++)
            {
                slot.Render(canvas, Width, Height, time += Subjects.FrameSeconds);
            }

            sampler.Every(i, 10);
        }
    }

    // A window being dragged: every frame a new size, so every frame a new backdrop.
    private static void Resize(SKSurface surface, Sampler sampler)
    {
        using var scene = Scene.Create("village");
        for (var i = 1; i <= 500; i++)
        {
            var width = Width - (i % 50 * 12);
            var height = Height - (i % 37 * 9);
            scene.Render(surface.Canvas, width, height, i * Subjects.FrameSeconds);
            sampler.Every(i, 10);
        }
    }

    // What SceneCanvas.DecodePhoto does on every slideshow step: decode the new bytes, never free the old image.
    private static void Photos(SKCanvas canvas, Sampler sampler, bool dispose)
    {
        using var scene = Scene.Create("village");
        byte[][] album = [.. Enumerable.Range(1, 4).Select(Subjects.PhotoJpeg)];
        SKImage? shown = null;
        var time = 0f;
        for (var i = 1; i <= 500; i++)
        {
            var next = SKImage.FromEncodedData(album[i % album.Length]);
            scene.SetPhoto(next, showForeground: true);

            // Through the 1.2 s crossfade, so the scene has let go of the old picture before it's freed.
            for (var f = 0; f < 45; f++)
            {
                scene.Render(canvas, Width, Height, time += Subjects.FrameSeconds);
            }

            if (dispose)
            {
                shown?.Dispose();
            }

            shown = next;
            sampler.Every(i, 10);
        }

        if (dispose)
        {
            shown?.Dispose();
        }
    }

    private static void Taps(SKCanvas canvas, Sampler sampler)
    {
        using var scene = Scene.Create("village");
        for (var i = 1; i <= 10_000; i++)
        {
            var time = i * Subjects.FrameSeconds;
            scene.Tap(Width * 0.5f, Height * 0.1f, Width, Height, time);
            scene.Render(canvas, Width, Height, time);
            sampler.Every(i, 500);
        }
    }
}

internal sealed class Sampler(string scenario)
{
    private readonly List<string> _rows = ["scenario,step,label,private_mb,working_set_mb,managed_mb,skia_cache_mb,gen0,gen1,gen2"];
    private readonly Process _process = Process.GetCurrentProcess();

    public static void Settle()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    public void Every(int step, int interval)
    {
        if (step % interval == 0)
        {
            Sample(step, "");
        }
    }

    public void Sample(int step, string label)
    {
        _process.Refresh();
        var row = string.Join(',',
            scenario,
            step,
            label,
            Mb(_process.PrivateMemorySize64),
            Mb(_process.WorkingSet64),
            Mb(GC.GetTotalMemory(false)),
            Mb(SKGraphics.GetResourceCacheTotalBytesUsed()),
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));
        _rows.Add(row);
        if (label.Length > 0)
        {
            Console.WriteLine(row);
        }
    }

    public void Save(string? path)
    {
        if (path is not null)
        {
            File.WriteAllLines(path, _rows);
        }
    }

    private static string Mb(long bytes) => (bytes / 1048576.0).ToString("F1", CultureInfo.InvariantCulture);
}
