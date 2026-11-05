using SkiaSharp;

namespace MerryCountdown.Scenes;

/// <summary>A see-through shower of confetti laid over a just-opened door.</summary>
public sealed class ConfettiScene : Scene
{
    private const int Pieces = 90;

    public override string Id => "confetti";

    protected override void PaintSky(SKCanvas canvas, int width, int height) =>
        canvas.Clear(SKColors.Transparent);

    protected override void PaintSnow(SKCanvas canvas, int width, int height, float time)
    {
    }

    protected override void PaintForegroundLife(SKCanvas canvas, int width, int height, float time)
    {
        var unit = Math.Min(width, height);
        Random random = new(2412);
        using SKPaint paint = new() { IsAntialias = true };

        for (var i = 0; i < Pieces; i++)
        {
            var x = random.NextSingle();
            var speed = 0.12f + (random.NextSingle() * 0.16f);
            var offset = random.NextSingle();
            var spin = 2 + (random.NextSingle() * 4);
            var sway = random.NextSingle() * MathF.Tau;
            var size = unit * (0.01f + (random.NextSingle() * 0.012f));
            paint.Color = Palette.SparkleColors[i % Palette.SparkleColors.Length];

            var progress = ((time * speed) + offset) % 1f;
            var px = (x * width) + (MathF.Sin((time * 1.5f) + sway) * unit * 0.025f);
            var py = (progress * (height + (size * 4))) - (size * 2);

            canvas.Save();
            canvas.Translate(px, py);
            canvas.RotateRadians((time * spin) + sway);
            canvas.Scale(1, MathF.Abs(MathF.Cos((time * spin) + sway)));
            canvas.DrawRect(-size, -size * 0.6f, size * 2, size * 1.2f, paint);
            canvas.Restore();
        }
    }
}
