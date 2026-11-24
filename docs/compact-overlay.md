# The compact overlay

To keep the countdown in view while using the PC for something else, open the palette button in the app bar on Home and choose **Mini window on top**. The app shrinks to a small always-on-top window showing the family's sign on their scene. The button in its corner, or Esc, brings back the window exactly as it was, maximized included.

It's only offered on the WinUI head. Uno's `AppWindow.SetPresenter` throws `NotSupportedException` for `CompactOverlay` on every other target, so `ICompactOverlayService.IsSupported` is false there and the look picker leaves the option out.

## How it fits together

- `CompactOverlayService` swaps the presenter and keeps the old one to put back. A new default presenter wouldn't remember being maximized.
- It sets the size itself, 420 by 236 DIPs. Windows sizes the overlay from the work area, which on an ultrawide gives a "small" window nearly 2000 pixels wide.
- It also turns the caption buttons white while active, and restores them afterwards. They sit on the scene here, and the light theme's black would disappear into a night sky.
- `WindowShell` hosts `CompactOverlayView` over everything, the same way as the Family Display. The app bar, which is the title bar on Windows, is collapsed underneath it, so `OverlayDragRegion` takes over as the title bar and the top edge still drags the window.
- `CompactOverlayViewModel` owns its own `CountdownSignViewModel`, like [Home and Family Display](./countdown-sign.md), so all three show the same sign.

## Staying light

This window runs next to someone's work, so its scene always paints at the Calm motion rate, whatever the setting says. The setting still decides whether the sign moves. Home's own scene is hidden behind it, so `CountdownViewModel.IsScenePaused` stops that one painting until the overlay closes.
