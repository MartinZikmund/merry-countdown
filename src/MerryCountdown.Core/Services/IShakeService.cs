namespace MerryCountdown.Core.Services;

/// <summary>Someone is shaking the device, or the app's window. Each head finds it its own way.</summary>
/// <remarks>Only listens while something is subscribed, so a sensor isn't kept awake for a page nobody sees.</remarks>
public interface IShakeService
{
    /// <summary>Raised on the UI thread again and again while a shake lasts.</summary>
    event EventHandler<ShakeEventArgs>? Shaken;
}

/// <summary>One moment of a shake. A new one each time, so a binding sees every push.</summary>
public sealed class ShakeEventArgs(double strength) : EventArgs
{
    /// <summary>From just over 0 for a gentle shake to 1 for a wild one.</summary>
    public double Strength { get; } = strength;
}

/// <summary>For heads that have nothing to shake.</summary>
public sealed class NoShakeService : IShakeService
{
    public event EventHandler<ShakeEventArgs>? Shaken
    {
        add { }
        remove { }
    }
}
