using Uno.Disposables;
using Windows.System.Display;

namespace MerryCountdown.Services;

internal sealed class DisplayRequestManager : IDisplayRequestManager
{
    private int _activeRequestCount;
    private int _generation;
    private readonly DisplayRequest _displayRequest = new();

    public IDisposable RequestActive()
    {
        var currentGeneration = _generation;
        _activeRequestCount++;
        _displayRequest.RequestActive();
        UpdateExecutionState();

        return Disposable.Create(() =>
        {
            // Only release if Clear() hasn't been called in between
            if (_generation == currentGeneration)
            {
                _activeRequestCount--;
                _displayRequest.RequestRelease();
                UpdateExecutionState();
            }
        });
    }

    public void Clear()
    {
        while (_activeRequestCount > 0)
        {
            _displayRequest.RequestRelease();
            _activeRequestCount--;
        }

        _generation++;
        UpdateExecutionState();
    }

    private void UpdateExecutionState()
    {
#if !WINAPPSDK
        // DisplayRequest is a no-op on Skia desktop, but a PC plugged into a TV still has to stay on.
        if (OperatingSystem.IsWindows())
        {
            const uint Continuous = 0x80000000;
            const uint SystemRequired = 0x00000001;
            const uint DisplayRequired = 0x00000002;
            SetThreadExecutionState(_activeRequestCount > 0 ? Continuous | SystemRequired | DisplayRequired : Continuous);
        }
#endif
    }

#if !WINAPPSDK
    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    private static extern uint SetThreadExecutionState(uint flags);
#endif
}
