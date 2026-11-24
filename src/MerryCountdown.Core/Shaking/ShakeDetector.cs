namespace MerryCountdown.Core.Shaking;

/// <summary>
/// Turns pushes into shakes: several quick pushes, each one back the way the last one came.
/// A drag, a slow move or a bump on the way never gets that far.
/// </summary>
/// <param name="weakest">The smallest push that counts at all.</param>
/// <param name="hardest">A push this hard or harder is a full-strength shake.</param>
public sealed class ShakeDetector(double weakest, double hardest)
{
    private const int PushesInAShake = 4;
    private const double MostSecondsBetweenPushes = 0.45;

    // Anything within 60 degrees of straight back counts as a reversal.
    private const double Opposite = -0.5;

    private readonly List<Stroke> _pushes = [];

    /// <returns>The strength of the shake, from just over 0 to 1, or null while there is no shake.</returns>
    public double? Push(double seconds, double x, double y, double z)
    {
        var size = Math.Sqrt((x * x) + (y * y) + (z * z));
        if (size < weakest)
        {
            return null;
        }

        Stroke push = new(seconds, x / size, y / size, z / size, size);
        if (_pushes.Count > 0 && !Reverses(_pushes[^1], push))
        {
            _pushes.Clear();
        }

        _pushes.Add(push);
        if (_pushes.Count > PushesInAShake)
        {
            _pushes.RemoveAt(0);
        }

        if (_pushes.Count < PushesInAShake)
        {
            return null;
        }

        var average = _pushes.Average(p => p.Size);
        return 0.3 + (0.7 * Math.Clamp((average - weakest) / (hardest - weakest), 0, 1));
    }

    private static bool Reverses(Stroke last, Stroke next) =>
        next.Seconds - last.Seconds <= MostSecondsBetweenPushes
        && (last.X * next.X) + (last.Y * next.Y) + (last.Z * next.Z) < Opposite;

    private readonly record struct Stroke(double Seconds, double X, double Y, double Z, double Size);
}

/// <summary>Finds a shake in the positions of a window someone is dragging, in DIPs.</summary>
public sealed class WindowShakeTracker
{
    private const double ShortestStroke = 30;
    private const double LongestStrokeSeconds = 0.4;

    // Stroke speeds in DIPs per second.
    private readonly ShakeDetector _detector = new(weakest: 400, hardest: 3000);

    private (double Seconds, double X, double Y)? _strokeStart;
    private (double Seconds, double X, double Y) _last;

    /// <returns>The strength of the shake under way, or null.</returns>
    public double? Moved(double seconds, double x, double y)
    {
        if (_strokeStart is not { } start || seconds - _last.Seconds > LongestStrokeSeconds)
        {
            _strokeStart = _last = (seconds, x, y);
            return null;
        }

        var (strokeX, strokeY) = (_last.X - start.X, _last.Y - start.Y);
        var (stepX, stepY) = (x - _last.X, y - _last.Y);
        var stroke = Math.Sqrt((strokeX * strokeX) + (strokeY * strokeY));
        double? shake = null;

        // The window turned back: a long enough stroke so far is one push of the shake, a short one just a wiggle.
        if ((strokeX * stepX) + (strokeY * stepY) < 0)
        {
            var duration = _last.Seconds - start.Seconds;
            if (stroke >= ShortestStroke && duration > 0 && duration <= LongestStrokeSeconds)
            {
                var speed = stroke / duration;
                shake = _detector.Push(_last.Seconds, strokeX / stroke * speed, strokeY / stroke * speed, 0);
            }

            _strokeStart = _last;
        }

        _last = (seconds, x, y);
        return shake;
    }
}

/// <summary>Finds a shake in accelerometer readings, in g with gravity included.</summary>
public sealed class MotionShakeTracker
{
    private const double GravitySeconds = 0.8;

    // A jolt this far from gravity, in g, is more than walking or turning the phone over.
    private const double Jolt = 1.2;

    private readonly ShakeDetector _detector = new(weakest: Jolt, hardest: 3);

    private (double X, double Y, double Z)? _gravity;
    private double _lastSeconds;
    private (double Seconds, double X, double Y, double Z, double Size)? _peak;

    /// <returns>The strength of the shake under way, or null.</returns>
    public double? Measured(double seconds, double x, double y, double z)
    {
        if (_gravity is not { } gravity)
        {
            _gravity = (x, y, z);
            _lastSeconds = seconds;
            return null;
        }

        var dt = Math.Max(0, seconds - _lastSeconds);
        _lastSeconds = seconds;
        var follow = dt / (GravitySeconds + dt);
        gravity = (gravity.X + ((x - gravity.X) * follow), gravity.Y + ((y - gravity.Y) * follow), gravity.Z + ((z - gravity.Z) * follow));
        _gravity = gravity;

        var (jx, jy, jz) = (x - gravity.X, y - gravity.Y, z - gravity.Z);
        var size = Math.Sqrt((jx * jx) + (jy * jy) + (jz * jz));
        if (size >= Jolt)
        {
            if (_peak is not { } peak || size > peak.Size)
            {
                _peak = (seconds, jx, jy, jz, size);
            }

            return null;
        }

        // The jolt is over: its hardest moment is the push.
        if (_peak is { } done)
        {
            _peak = null;
            return _detector.Push(done.Seconds, done.X, done.Y, done.Z);
        }

        return null;
    }
}
