using FluentAssertions;
using MerryCountdown.Core.Shaking;

namespace MerryCountdown.Core.Tests;

[TestClass]
public sealed class ShakeDetectorTests
{
    private const double WindowStep = 1 / 60.0;
    private const double SensorStep = 1 / 50.0;

    // Window positions in DIPs, the way AppWindow reports them while someone drags it.
    private static List<double> Window(double seconds, Func<double, (double X, double Y)> position)
    {
        WindowShakeTracker tracker = new();
        List<double> shakes = [];
        for (var t = 0.0; t <= seconds; t += WindowStep)
        {
            var (x, y) = position(t);
            if (tracker.Moved(t, x, y) is { } strength)
            {
                shakes.Add(strength);
            }
        }

        return shakes;
    }

    // Accelerometer readings in g, gravity included, the way Windows.Devices.Sensors reports them.
    private static List<double> Device(double seconds, Func<double, (double X, double Y, double Z)> reading)
    {
        MotionShakeTracker tracker = new();
        List<double> shakes = [];
        for (var t = 0.0; t <= seconds; t += SensorStep)
        {
            var (x, y, z) = reading(t);
            if (tracker.Measured(t, x, y, z) is { } strength)
            {
                shakes.Add(strength);
            }
        }

        return shakes;
    }

    private static double Wave(double t, double hertz) => Math.Sin(t * hertz * Math.Tau);

    [TestMethod]
    public void Window_ShakenSideToSide_IsAShake() =>
        Window(1.2, t => (600 + (120 * Wave(t, 3)), 300)).Should().NotBeEmpty();

    [TestMethod]
    public void Window_ShakenUpAndDown_IsAShake() =>
        Window(1.2, t => (600, 300 + (90 * Wave(t, 3.5)))).Should().NotBeEmpty();

    [TestMethod]
    public void Window_ShakenWithAShakyHand_IsAShake() =>
        Window(1.2, t => (600 + (120 * Wave(t, 3)) + (3 * Wave(t, 23)), 300 + (6 * Wave(t, 2)))).Should().NotBeEmpty();

    [TestMethod]
    public void Window_DraggedAcrossTheScreen_IsNoShake() =>
        Window(1.5, t => (100 + (900 * t / 1.5), 300 + (40 * t))).Should().BeEmpty();

    [TestMethod]
    public void Window_DraggedWithJitter_IsNoShake() =>
        Window(1.5, t => (100 + (700 * t) + (3 * Wave(t, 11)), 300 + (2 * Wave(t, 7)))).Should().BeEmpty();

    [TestMethod]
    public void Window_MovedSlowlyBackAndForth_IsNoShake() =>
        Window(4, t => (600 + (250 * Wave(t, 0.4)), 300)).Should().BeEmpty();

    [TestMethod]
    public void Window_NudgedAFewPixels_IsNoShake() =>
        Window(1.5, t => (600 + (6 * Wave(t, 5)), 300)).Should().BeEmpty();

    [TestMethod]
    public void Window_RockedGentlyAFewTimes_IsNoShake() =>
        Window(2, t => (600 + (20 * Wave(t, 3)), 300)).Should().BeEmpty();

    [TestMethod]
    public void Window_SwungTwiceAndLeft_IsNoShake() =>
        Window(1.5, t => (600 + (150 * Wave(Math.Min(t, 0.7), 2.5)), 300)).Should().BeEmpty();

    [TestMethod]
    public void Window_DraggedThereAndBackOnce_IsNoShake() =>
        Window(1.2, t => (600 + (300 * Math.Sin(Math.Min(t, 0.8) / 0.8 * Math.PI)), 300)).Should().BeEmpty();

    [TestMethod]
    public void Window_ShakenHarder_IsAStrongerShake()
    {
        var gentle = Window(1.2, t => (600 + (50 * Wave(t, 3)), 300));
        var hard = Window(1.2, t => (600 + (200 * Wave(t, 4)), 300));

        gentle.Should().NotBeEmpty();
        hard.Max().Should().BeGreaterThan(gentle.Max());
        hard.Should().OnlyContain(s => s > 0 && s <= 1);
    }

    [TestMethod]
    public void Window_KeepsReportingWhileTheShakeLasts()
    {
        WindowShakeTracker tracker = new();
        List<double> times = [];
        for (var t = 0.0; t <= 3; t += WindowStep)
        {
            var x = t < 2 ? 600 + (120 * Wave(t, 3)) : 600;
            if (tracker.Moved(t, x, 300) is not null)
            {
                times.Add(t);
            }
        }

        times.Should().HaveCountGreaterThan(5);
        times.Should().Contain(t => t > 1.5, "the shake went on for two seconds");
        times.Should().OnlyContain(t => t <= 2.05, "it stopped when the shaking did");
    }

    [TestMethod]
    public void Window_SamplesAfterAPause_StartAfresh()
    {
        WindowShakeTracker tracker = new();
        List<double> times = [];

        // Two strokes, a long rest, then two more: never four quick ones in a row.
        double[] stops = [0, 0.15, 0.3, 2.3, 2.45, 2.6];
        double[] xs = [600, 800, 600, 800, 600, 800];
        for (var i = 1; i < stops.Length; i++)
        {
            for (var t = stops[i - 1]; t < stops[i]; t += WindowStep)
            {
                var progress = Math.Min(1, (t - stops[i - 1]) / 0.15);
                if (tracker.Moved(t, xs[i - 1] + ((xs[i] - xs[i - 1]) * progress), 300) is not null)
                {
                    times.Add(t);
                }
            }
        }

        times.Should().BeEmpty();
    }

    [TestMethod]
    public void Device_Shaken_IsAShake() =>
        Device(1.2, t => (2.2 * Wave(t, 4.5), 0.1, 1)).Should().NotBeEmpty();

    [TestMethod]
    public void Device_ShakenWhileHeldUpright_IsAShake() =>
        Device(1.2, t => (0, -1 + (2 * Wave(t, 5)), 0.2)).Should().NotBeEmpty();

    [TestMethod]
    public void Device_LyingStill_IsNoShake() =>
        Device(3, _ => (0.01, -0.02, -1)).Should().BeEmpty();

    [TestMethod]
    public void Device_CarriedWhileWalking_IsNoShake() =>
        Device(5, t => (0.15 * Wave(t, 1), -1 + (0.45 * Wave(t, 2)), 0.2 + (0.2 * Wave(t, 2)))).Should().BeEmpty();

    [TestMethod]
    public void Device_PickedUpAndTurned_IsNoShake() =>
        Device(2, t =>
        {
            var turn = Math.Clamp((t - 0.5) / 0.4, 0, 1) * Math.PI / 2;
            return (0, -Math.Sin(turn), -Math.Cos(turn));
        }).Should().BeEmpty();

    [TestMethod]
    public void Device_PutDownWithABump_IsNoShake() =>
        Device(2, t => (0, 0, -1 + (Math.Abs(t - 1) < 0.03 ? 2.5 : 0))).Should().BeEmpty();

    [TestMethod]
    public void Device_KnockedOnRepeatedly_IsNoShake() =>
        Device(2, t => (0, 0, -1 + (t % 0.2 < 0.03 ? 2.5 : 0))).Should().BeEmpty();

    [TestMethod]
    public void Device_ShakenHarder_IsAStrongerShake()
    {
        var gentle = Device(1.2, t => (1.6 * Wave(t, 4), 0, -1));
        var hard = Device(1.2, t => (3.5 * Wave(t, 5), 0, -1));

        gentle.Should().NotBeEmpty();
        hard.Max().Should().BeGreaterThan(gentle.Max());
        hard.Should().OnlyContain(s => s > 0 && s <= 1);
    }
}
