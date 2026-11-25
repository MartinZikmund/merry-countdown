using SkiaSharp;

namespace MerryCountdown.Scenes;

public sealed class VillageScene : Scene
{
    private const float VillageGround = 0.74f;
    private const float TreeReaction = 2.5f;
    private const float HouseReaction = 2f;
    private const int MostHouses = 64;
    private const float ShakeSettle = 2.5f;
    private const int TreeLights = 26;

    // Where each light hangs: how far down the tree, and how far out towards the tips of the branches.
    private static readonly (float Down, float Out)[] _lightSpots = CreateLightSpots();

    private readonly float[] _houseTapped = Enumerable.Repeat(float.NegativeInfinity, MostHouses).ToArray();
    private float _treeTapped = float.NegativeInfinity;

    public override string Id => "village";

    protected override void PaintSky(SKCanvas canvas, int width, int height)
    {
        var unit = Math.Min(width, height);
        Painter.Sky(canvas, width, height, Palette.Night, Palette.Dusk, Palette.Haze);
        Painter.Stars(canvas, width, height, 0.65f, 7, 170);
        Painter.Moon(canvas, width * 0.82f, height * 0.17f, unit * 0.075f);
    }

    protected override void PaintForeground(SKCanvas canvas, int width, int height)
    {
        var unit = Math.Min(width, height);
        Painter.Hill(canvas, width, height, height * 0.62f, height * 0.05f, 1.4f, 0.6f, Palette.SnowDeep, Palette.Haze);
        ForestLine(canvas, width, height, height * 0.62f, 1.4f, 0.6f, height * 0.05f);

        var ground = height * VillageGround;
        Painter.Hill(canvas, width, height, ground, height * 0.02f, 1f, 2.2f, Palette.SnowShade, Palette.SnowDeep);
        PaintVillage(canvas, width, height);

        Painter.Hill(canvas, width, height, height * 0.87f, height * 0.025f, 1.6f, 4f, Palette.Snow, Palette.SnowShade);
        Painter.Pine(canvas, width * 0.06f, height * 0.9f, unit * 0.34f, Palette.PineDark, Palette.Snow);
        Painter.Pine(canvas, width * 0.95f, height * 0.91f, unit * 0.28f, Palette.PineDark, Palette.Snow);
    }

    protected override void PaintSkyLife(SKCanvas canvas, int width, int height, float time) =>
        Painter.Twinkles(canvas, width, height, 0.55f, time, 19, 14);

    protected override void PaintForegroundLife(SKCanvas canvas, int width, int height, float time)
    {
        PaintLitHouses(canvas, width, height, time);
        PaintTreeLights(canvas, width, height, time);
        PaintSlidingSnow(canvas, width, height, time);
    }

    protected override IEnumerable<SceneElement> ForegroundElements(int width, int height, float time)
    {
        var (x, ground, treeHeight) = Tree(width, height);
        var halfTree = treeHeight * 0.3f;
        yield return new SceneElement(new SKRect(x - halfTree, ground - treeHeight, x + halfTree, ground), tappedAt => _treeTapped = tappedAt);

        var houses = LayOutHouses(width, height);
        for (var i = 0; i < houses.Count && i < MostHouses; i++)
        {
            var index = i;
            yield return new SceneElement(houses[i].Area, tappedAt => _houseTapped[index] = tappedAt);
        }
    }

    private static (float X, float Ground, float Height) Tree(int width, int height) =>
        (width * 0.5f, height * 0.94f, Math.Min(width, height) * 0.34f);

    private static (float Down, float Out)[] CreateLightSpots()
    {
        Random random = new(99);
        var spots = new (float, float)[TreeLights];
        for (var i = 0; i < TreeLights; i++)
        {
            var along = random.NextSingle();
            var side = (random.NextSingle() * 2) - 1;
            spots[i] = (0.08f + (along * 0.74f), side * 0.8f);
        }

        return spots;
    }

    /// <summary>The light's place on a tree: on its branches, never in the air beside it.</summary>
    internal static SKPoint TreeLight(int index, float x, float ground, float treeHeight)
    {
        var (down, toTip) = _lightSpots[index];
        return new SKPoint(x + (toTip * Painter.PineHalfWidth(down) * treeHeight), ground - treeHeight + (down * treeHeight));
    }

    private static void ForestLine(SKCanvas canvas, int width, int height, float baseline, float waves, float phase, float swell)
    {
        Random random = new(42);
        var unit = Math.Min(width, height);
        var spacing = unit * 0.05f;
        for (var x = -spacing; x < width + spacing; x += spacing * (0.6f + random.NextSingle()))
        {
            var tree = unit * (0.07f + (random.NextSingle() * 0.05f));
            var ground = Painter.HillY(x, width, baseline, swell, waves, phase) + (unit * 0.012f);
            Painter.Pine(canvas, x, ground, tree, Palette.Pine.WithAlpha(235), Palette.SnowShade);
        }
    }

    private static void PaintVillage(SKCanvas canvas, int width, int height)
    {
        foreach (var house in LayOutHouses(width, height))
        {
            Painter.House(canvas, house.X, house.Ground, house.Width, house.Height, house.Wall, Palette.Snow, house.Windows, Palette.Candle);
        }
    }

    private void PaintLitHouses(SKCanvas canvas, int width, int height, float time)
    {
        List<House>? houses = null;

        // Shaken calmly, every window glows softly instead of the snow sliding off.
        var shaken = CalmMotion ? Shaken(time, ShakeSettle) * 0.3f : 0;
        for (var i = 0; i < MostHouses; i++)
        {
            var left = Math.Max(Remaining(_houseTapped[i], time, HouseReaction), shaken);
            if (left <= 0)
            {
                continue;
            }

            houses ??= LayOutHouses(width, height);
            if (i >= houses.Count)
            {
                continue;
            }

            // Everyone inside rushes to the window.
            var alpha = (byte)((CalmMotion ? 110 : 235) * left);
            foreach (var window in houses[i].Windows)
            {
                Painter.Glow(canvas, window.MidX, window.MidY, window.Width * 3.4f, Palette.Candle, alpha);
                Painter.Glow(canvas, window.MidX, window.MidY, window.Width * 1.2f, SKColors.White, alpha);
            }
        }
    }

    // Clumps of snow slide off the roofs and the tree's branches for as long as the shake lasts.
    private void PaintSlidingSnow(SKCanvas canvas, int width, int height, float time)
    {
        var shaken = Shaken(time, ShakeSettle);
        if (shaken <= 0 || CalmMotion)
        {
            return;
        }

        using SKPaint paint = new() { IsAntialias = true };
        var unit = Math.Min(width, height);
        var alpha = Math.Min(1, shaken * 1.5f);
        var houses = LayOutHouses(width, height);
        for (var i = 0; i < houses.Count; i++)
        {
            var house = houses[i];
            var eave = house.Ground - house.Height;
            var reach = (house.Width / 2) + (house.Width * 0.14f);
            for (var side = -1; side <= 1; side += 2)
            {
                Clumps(canvas, paint, house.X + (side * reach * 0.9f), eave, house.Height, house.Width * 0.07f, time, (i * 2) + side, alpha);
            }
        }

        var (x, ground, treeHeight) = Tree(width, height);
        for (var tier = 0; tier < 3; tier++)
        {
            var y = ground - (treeHeight * (0.7f - (tier * 0.22f)));
            var halfWidth = treeHeight * (0.1f + (tier * 0.07f));
            for (var side = -1; side <= 1; side += 2)
            {
                Clumps(canvas, paint, x + (side * halfWidth), y, treeHeight * 0.2f, unit * 0.008f, time, 100 + (tier * 2) + side, alpha);
            }
        }
    }

    private static void Clumps(SKCanvas canvas, SKPaint paint, float x, float y, float fall, float size, float time, int seed, float alpha)
    {
        Random random = new(seed);
        for (var i = 0; i < 4; i++)
        {
            var progress = ((time * (1.4f + random.NextSingle())) + random.NextSingle()) % 1f;
            var drift = (random.NextSingle() - 0.5f) * size * 3;
            paint.Color = Palette.Snow.WithAlpha((byte)(255 * alpha * (1 - (progress * progress))));
            canvas.DrawCircle(x + (drift * progress), y + (progress * progress * fall), size * (1 - (progress * 0.4f)), paint);
        }
    }

    private static List<House> LayOutHouses(int width, int height)
    {
        var ground = height * VillageGround;
        var unit = Math.Min(width, height);
        var houseCount = Math.Max(4, (int)(width / (unit * 0.2f)));
        SKColor[] walls = [new(0x8C, 0x3B, 0x3B), new(0x3E, 0x5C, 0x7E), new(0xA2, 0x6B, 0x3F), new(0x4B, 0x6B, 0x57), new(0x6F, 0x46, 0x7D)];
        Random random = new(5);
        List<House> houses = new(houseCount);

        for (var i = 0; i < houseCount; i++)
        {
            var x = (width * (i + 0.5f) / houseCount) + ((random.NextSingle() - 0.5f) * unit * 0.03f);
            var houseWidth = unit * (0.13f + (random.NextSingle() * 0.05f));
            var houseHeight = houseWidth * (0.62f + (random.NextSingle() * 0.2f));
            var groundHere = Painter.HillY(x, width, ground, height * 0.02f, 1f, 2.2f) + (unit * 0.012f);

            var windows = random.Next(1, 3);
            var windowWidth = houseWidth * 0.2f;
            var panes = new SKRect[windows];
            for (var w = 0; w < windows; w++)
            {
                var wx = x - (windows * windowWidth * 0.75f) + (w * windowWidth * 1.5f) + (windowWidth * 0.25f);
                panes[w] = SKRect.Create(wx, groundHere - (houseHeight * 0.72f), windowWidth, windowWidth * 1.15f);
            }

            houses.Add(new House(x, groundHere, houseWidth, houseHeight, walls[i % walls.Length], panes));
        }

        return houses;
    }

    private void PaintTreeLights(SKCanvas canvas, int width, int height, float time)
    {
        // The town tree stands in the front so the sky stays clear for the big number.
        var unit = Math.Min(width, height);
        var (x, ground, treeHeight) = Tree(width, height);
        var shaken = Shaken(time, ShakeSettle);
        var left = Math.Max(Remaining(_treeTapped, time, TreeReaction), shaken);
        var growth = 1 + (left * (CalmMotion ? 0.35f : 1f));
        var jingle = CalmMotion ? 0 : shaken * unit * 0.005f;

        Painter.Glow(canvas, x, ground - (treeHeight * 0.5f), treeHeight * 0.85f, Palette.Candle, 40);
        Painter.Pine(canvas, x, ground, treeHeight, Palette.Pine, Palette.Snow);

        using SKPaint paint = new() { IsAntialias = true };
        for (var i = 0; i < TreeLights; i++)
        {
            var spot = TreeLight(i, x, ground, treeHeight);
            var ly = spot.Y;
            var lx = spot.X + (jingle * MathF.Sin((time * 31f) + (i * 2.3f)));
            var color = Palette.Lights[i % Palette.Lights.Length];
            var speed = left > 0 && !CalmMotion ? 14f : 2.1f;
            var pulse = 0.55f + (0.45f * MathF.Sin((time * speed) + (i * 1.7f)));
            var radius = unit * 0.0075f * growth;
            Painter.Glow(canvas, lx, ly, radius * 3.4f, color, (byte)(120 * pulse));
            paint.Color = color.WithAlpha((byte)(150 + (105 * pulse)));
            canvas.DrawCircle(lx, ly, radius, paint);
        }

        paint.Color = Palette.Candle;
        var shine = (1 + (0.08f * MathF.Sin(time * 1.5f))) * (1 + ((growth - 1) * 0.5f));
        Painter.Glow(canvas, x, ground - treeHeight, unit * 0.07f * shine, Palette.Candle, 170);
        Painter.Star(canvas, paint, x, ground - treeHeight, unit * 0.032f * shine, 5, 0.45f, 0);
    }

    private sealed record House(float X, float Ground, float Width, float Height, SKColor Wall, SKRect[] Windows)
    {
        public SKRect Area
        {
            get
            {
                var overhang = Width * 0.14f;
                return new SKRect(X - (Width / 2) - overhang, Ground - Height - (Width * 0.46f), X + (Width / 2) + overhang, Ground);
            }
        }
    }
}
