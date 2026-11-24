# Keeping the screen on

**Settings > Look and motion > Keep the screen on** stops the screen sleeping while the app is open on any page, including the mini window. It is off by default and saved per device (`IAppPreferences.KeepScreenOn`). Family Display keeps the screen on whatever this says.

## How it decides

`ScreenWake` (Core) holds one `IDisplayRequestManager.RequestActive()` request while both are true:

- the window is visible, and
- Family Display is active or the setting is on.

Turning the setting off during Family Display, or leaving Family Display with the setting on, changes nothing: the one request stays. `ScreenWakeTests` covers the combinations with a fake request manager.

`WindowShell` creates it per window and tells it when the window is shown or hidden. A minimized or backgrounded app lets the screen sleep, Family Display included, and takes the request again when it comes back.

## What the heads do

| Head | Visibility signal | Holds the screen with |
| --- | --- | --- |
| WinUI | `AppWindow.Changed` (`IsVisible`, minimized presenter) and `Window.VisibilityChanged` | `DisplayRequest` |
| Skia desktop, Windows | `Window.VisibilityChanged` | `SetThreadExecutionState`, see below |
| Android, iOS | `Window.VisibilityChanged` (app goes to the background) | `DisplayRequest` |
| Other Uno heads | `Window.VisibilityChanged`, where the head raises it | `DisplayRequest`, which may do nothing |

`DisplayRequest` is a no-op on Skia desktop, so `DisplayRequestManager` also calls `SetThreadExecutionState` there. It does that when the count of requests goes from 0 to 1 and back, so every caller gets it. The call is per thread, so requests must be taken and released on the UI thread, which they are.

WinUI does not raise `Window.VisibilityChanged` for minimize, hence the `AppWindow.Changed` handler on that head.
