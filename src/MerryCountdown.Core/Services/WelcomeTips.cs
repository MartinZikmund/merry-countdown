using MerryCountdown.Services.Settings;

namespace MerryCountdown.Core.Services;

public enum WelcomeTip
{
    None,
    Customize,
    Settings,
}

/// <summary>The two tips a new family gets once: where to change the look, then where the settings are.</summary>
public sealed class WelcomeTips(IAppPreferences preferences)
{
    public WelcomeTip Current { get; private set; }

    public event EventHandler? Changed;

    public void Begin()
    {
        if (Current == WelcomeTip.None && !preferences.HasSeenWelcomeTips)
        {
            Show(WelcomeTip.Customize);
        }
    }

    public void Next()
    {
        if (Current == WelcomeTip.Customize)
        {
            Show(WelcomeTip.Settings);
        }
        else
        {
            Finish();
        }
    }

    /// <summary>Closing a tip any way ends the tour: a family who waved it away shouldn't meet it again.</summary>
    public void Finish()
    {
        if (Current == WelcomeTip.None)
        {
            return;
        }

        preferences.HasSeenWelcomeTips = true;
        Show(WelcomeTip.None);
    }

    private void Show(WelcomeTip tip)
    {
        Current = tip;
        Changed?.Invoke(this, EventArgs.Empty);
    }
}
