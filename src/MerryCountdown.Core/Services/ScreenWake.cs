using MerryCountdown.Services;
using MerryCountdown.Services.Settings;

namespace MerryCountdown.Core.Services;

/// <summary>Decides when the screen must not sleep: Family Display, or the setting, but only while someone can see the window.</summary>
public interface IScreenWake
{
    /// <summary>The "Keep the screen on" setting. Saved when changed.</summary>
    bool KeepScreenOn { get; set; }

    /// <summary>False while the window is minimized, hidden or in the background.</summary>
    void SetWindowVisible(bool isVisible);
}

public sealed class ScreenWake : IScreenWake, IDisposable
{
    private readonly IDisplayModeService _display;
    private readonly IAppPreferences _preferences;
    private readonly IDisplayRequestManager _requests;
    private IDisposable? _hold;
    private bool _isWindowVisible = true;

    public ScreenWake(IDisplayModeService display, IAppPreferences preferences, IDisplayRequestManager requests)
    {
        _display = display;
        _preferences = preferences;
        _requests = requests;
        _display.Changed += OnDisplayChanged;
        Update();
    }

    public bool KeepScreenOn
    {
        get => _preferences.KeepScreenOn;
        set
        {
            _preferences.KeepScreenOn = value;
            Update();
        }
    }

    public void SetWindowVisible(bool isVisible)
    {
        _isWindowVisible = isVisible;
        Update();
    }

    public void Dispose()
    {
        _display.Changed -= OnDisplayChanged;
        _hold?.Dispose();
        _hold = null;
    }

    private void OnDisplayChanged(object? sender, EventArgs e) => Update();

    private void Update()
    {
        var shouldHold = _isWindowVisible && (_display.IsActive || _preferences.KeepScreenOn);
        if (shouldHold && _hold is null)
        {
            _hold = _requests.RequestActive();
        }
        else if (!shouldHold && _hold is not null)
        {
            _hold.Dispose();
            _hold = null;
        }
    }
}
