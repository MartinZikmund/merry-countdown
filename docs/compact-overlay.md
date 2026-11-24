# The compact overlay

To keep the countdown in view while using the PC for something else, open the palette button in the app bar on Home and choose **Mini window on top**. The app shrinks to a small always-on-top window showing the family's sign on their scene. Drag any edge or corner to make it bigger or smaller. The button in its corner, or Esc, brings back the window exactly as it was, maximized included.

It's only offered on the WinUI head. `ICompactOverlayService.IsSupported` is false everywhere else and the look picker leaves the option out.

## How it fits together

- `CompactOverlayService` swaps in an always-on-top `OverlappedPresenter` with no minimize or maximize button. Not `AppWindowPresenterKind.CompactOverlay`, despite the name: that window has no sizing frame, so it can't be resized, and it puts the frame straight back if you add one.
- Both presenters are the same kind, so Windows wouldn't bring the old bounds back by itself. The service keeps the old presenter, its bounds and whether it was maximized, and puts all three back.
- It starts at 420 by 236 DIPs and won't go below 240 by 150.
- It also turns the caption buttons white while active, and restores them afterwards. They sit on the scene here, and the light theme's black would disappear into a night sky.
- `WindowShell` hosts `CompactOverlayView` over everything, the same way as the Family Display. The app bar, which is the title bar on Windows, is collapsed underneath it, so `OverlayDragRegion` takes over as the title bar and the top edge still drags the window.
- `CompactOverlayViewModel` owns its own `CountdownSignViewModel`, like [Home and Family Display](./countdown-sign.md), so all three show the same sign.

## Sign and scene share the window

The sign scales with the window but only ever gets the top 45% of it. The view tells the scene how much that is through `SceneCanvas.CoveredTop`, and scenes with something in the middle (the snow globe, the fireplace) shrink it to fit underneath. Home does the same, which matters in a short window or a phone on its side.

## Staying light

This window runs next to someone's work, so its scene always paints at the Calm motion rate, whatever the setting says. The setting still decides whether the sign moves. Home's own scene is hidden behind it, so `CountdownViewModel.IsScenePaused` stops that one painting until the overlay closes.
