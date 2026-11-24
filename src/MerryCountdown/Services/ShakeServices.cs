using System.Diagnostics;
using MerryCountdown.Core.Shaking;
using Microsoft.UI.Windowing;
#if __ANDROID__ || __IOS__
using Microsoft.UI.Dispatching;
using Windows.Devices.Sensors;
#endif

namespace MerryCountdown.Services;

/// <summary>Raises <see cref="Shaken"/> only while someone listens, and starts and stops listening to the hardware with it.</summary>
public abstract class ShakeServiceBase : IShakeService
{
    private EventHandler<ShakeEventArgs>? _shaken;

    public event EventHandler<ShakeEventArgs>? Shaken
    {
        add
        {
            var wasIdle = _shaken is null;
            _shaken += value;
            if (wasIdle && _shaken is not null)
            {
                Start();
            }
        }

        remove
        {
            _shaken -= value;
            if (_shaken is null)
            {
                Stop();
            }
        }
    }

    protected abstract void Start();

    protected abstract void Stop();

    protected void Raise(double strength) => _shaken?.Invoke(this, new ShakeEventArgs(strength));
}

/// <summary>Someone grabs the window by its title bar and shakes it. Works wherever the window reports its moves.</summary>
public sealed class WindowShakeService(IWindowShellProvider shells, IXamlRootProvider xamlRoots) : ShakeServiceBase
{
    private WindowShakeTracker _tracker = new();
    private AppWindow? _window;

    protected override void Start()
    {
        _window = shells.Window.AppWindow;
        _tracker = new();
        _window.Changed += OnChanged;
    }

    protected override void Stop()
    {
        if (_window is not null)
        {
            _window.Changed -= OnChanged;
            _window = null;
        }
    }

    private void OnChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (!args.DidPositionChange)
        {
            return;
        }

        // AppWindow works in physical pixels; the tracker's distances are in DIPs, so a 4K screen shakes the same.
        var scale = xamlRoots.XamlRoot.RasterizationScale is > 0 and var s ? s : 1;
        var seconds = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        if (_tracker.Moved(seconds, sender.Position.X / scale, sender.Position.Y / scale) is { } strength)
        {
            Raise(strength);
        }
    }
}

#if __ANDROID__ || __IOS__
/// <summary>Someone shakes the phone or tablet. No permission needed on either platform.</summary>
public sealed class MotionShakeService : ShakeServiceBase
{
    private const uint ReportMilliseconds = 20;

    private readonly Accelerometer? _sensor = Accelerometer.GetDefault();
    private MotionShakeTracker _tracker = new();
    private DispatcherQueue? _ui;

    protected override void Start()
    {
        if (_sensor is null)
        {
            return;
        }

        _ui = DispatcherQueue.GetForCurrentThread();
        _tracker = new();
        _sensor.ReportInterval = ReportMilliseconds;
        _sensor.ReadingChanged += OnReading;
    }

    protected override void Stop()
    {
        if (_sensor is not null)
        {
            _sensor.ReadingChanged -= OnReading;
        }
    }

    // Readings arrive on a sensor thread, one at a time, so only the result needs to hop to the UI.
    private void OnReading(Accelerometer sender, AccelerometerReadingChangedEventArgs args)
    {
        var reading = args.Reading;
        var seconds = reading.Timestamp.ToUnixTimeMilliseconds() / 1000.0;
        if (_tracker.Measured(seconds, reading.AccelerationX, reading.AccelerationY, reading.AccelerationZ) is { } strength)
        {
            _ui?.TryEnqueue(() => Raise(strength));
        }
    }
}
#endif
