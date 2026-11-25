# Drawing a scene

A scene is painted in code, in layers, and some layers are cached. Most drawing mistakes come from forgetting which
layer something is in, or from checking only one window shape. This page is the list to go through when you change how
a scene looks.

## The layers, back to front

| Layer | Method | Painted | Hidden by a photo |
| --- | --- | --- | --- |
| Sky | `PaintSky` | once, into the cached backdrop | yes |
| Foreground | `PaintForeground` | once, into the cached backdrop | only when the family turns the foreground off |
| Sky life | `PaintSkyLife` | every frame | yes |
| Foreground life | `PaintForegroundLife` | every frame | with the foreground |
| Snow, tap sparkles | `Scene` | every frame | no |

Everything painted every frame lands **on top of** the whole cached backdrop. So a moving thing can never be behind a
still thing unless you make it so.

## Rules

- **Clip what moves behind something still.** The globe's snow and shine are painted per frame, and its base is in the
  backdrop. `SnowGlobeScene.Glass` is the outline of the glass that shows above the base, and both the cached world
  and the per-frame snow are clipped to it. Build such a path once and keep it (free it in `Release`); don't rebuild
  it every frame.
- **Light goes on before what it must not touch.** `Painter.Window` paints a wide glow around the pane.
  `Painter.House` takes the windows and paints them before the roof, so the wall is lit and the snow stays white. The
  train's carriages do the same. A glow that is part of a reaction (a tapped house, the cabin's lamps) may spill: it is
  meant to flood the place for a moment.
- **Put things on the shape, not on a box around it.** The village tree's lights use `Painter.PineHalfWidth`, the
  width the pine really has at that height, so none hangs in the air beside it. `VillageScene.TreeLight` is the one
  place that decides where a light is.
- **Keep to the thing you stand on.** Lines and tilted shapes run past edges: clip the bricks to the wall, and place
  the logs so their tilted ends stay inside the firebox.
- **Size from both sides of the screen.** A centrepiece sized from the width alone runs off a phone held upright.
  `FireplaceScene.Geometry` sizes the hearth so the presents beside it still fit; `SnowGlobeScene.Geometry` does the
  same for the glass. Both also shrink under `CoveredTop`, see [compact-overlay.md](./compact-overlay.md).
- **Don't lean on the sky's colour.** With a photo behind the foreground, an edge that was dark-on-dark is suddenly
  dark on a bright picture. Anything in `PaintForeground` has to be a complete, opaque shape.

Tap targets come from the same layout methods as the painting ([scene-taps.md](./scene-taps.md)), so a layout change
moves them too. `SceneInteractionTests` checks every scene has something to tap at each screen shape, and
`FireplaceDrawingTests` that all of the fireplace's are on screen.

## Looking at it

Scenes render without a window, so look at PNGs before and after a change:

```bash
dotnet run -c Release --project tools/SceneBench -- render globe out/globe 1180 860
dotnet run -c Release --project tools/SceneBench -- render photo+globe out/globe-photo 390 844
dotnet run -c Release --project tools/SceneBench -- compare out/before out/after
```

`render` writes a few moments of the subject, lively and calm, idle and tapped. A subject is a scene id, `confetti`,
`photo` or `photo+<scene id>` for that scene's foreground in front of a photo. `compare` says how far two sets of
those PNGs are apart, which also shows what a change did *not* touch.

Shapes worth checking every time: a desktop window (1180 by 860), a phone upright (390 by 844 and 320 by 568), a TV
(1920 by 1080) and the mini window (about 360 by 220, with `CoveredTop` at 0.45).

## Tests

The rules above that can be checked by pixel or by geometry are in `tests/MerryCountdown.Scenes.Tests`:
`GlobeDrawingTests` (nothing moves over the base), `RoofDrawingTests` (roofs stay white above lit windows),
`VillageDrawingTests` (every light is on the tree) and `FireplaceDrawingTests` (nothing spills onto the floor, and
everything tappable is on screen). Add to them when you fix a drawing bug. If a fix adds work per frame, measure it as
[performance.md](./performance.md) describes.
