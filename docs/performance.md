# Performance and memory

The app is meant to run for a month: Family Display on a TV, the mini window next to someone's work. This page says
how to measure what that costs, what was measured in November 2026 (issue #40), what that fixed, and what nobody has
measured yet.

## Measuring the scenes

Scenes are pure SkiaSharp, so they can be timed without a window. `tools/SceneBench` is a small console program for
that. Build it in Release; run one memory scenario per process so one doesn't inherit another's heap.

```powershell
dotnet build tools/SceneBench/SceneBench.csproj -c Release
$bench = "tools/SceneBench/bin/Release/net10.0/SceneBench.dll"

dotnet $bench frames 2000 frames.csv          # every scene, confetti and a photo, 1080p and 4K, Calm motion on and off
dotnet $bench memory swap memory-swap.csv     # one scenario: steady, swap, swap-dispose, swap-slot, resize, photos, photos-dispose, taps
dotnet $bench render aurora before/           # PNGs of a few frames...
dotnet $bench compare before/ after/          # ...to check a speed-up didn't change the picture
```

`frames` reports mean, 99th percentile and worst frame time and managed bytes allocated per frame. It draws on a CPU
(raster) canvas, so the times are what a software-rendered head pays; the Windows head paints opaque scenes on the GPU
and pays less. Use the numbers to compare scenes and to compare before and after a change, not as a promise about any
one device.

`SceneAllocationTests` keeps every scene under 16 KB of managed allocation per frame at 1080p, so a shader or path built
per pixel column shows up as a failing test.

## Measuring the app

There is no checked-in script for the app. What was done for #40, on the Windows head, Debug build:

- **Churn**: a UI Automation loop against the app's own PID. One round is Home, Doors, Wishes, Settings, Family Display
  with five scene changes, back to Home; every fifth round also opens and closes the mini window. 200 rounds, sampling
  private bytes, working set, handles, threads and GDI/USER objects every 10 rounds.
- **Heap counts**: `dotnet-gcdump collect -p <pid>` before and after the churn, and `dotnet-gcdump report` for the live
  count of each view, view model and scene type. A gcdump runs a full GC first, so these are live objects.
- **Roots**: `dotnet-dump collect --type Heap`, then `gcroot` on one leaked object, shows what keeps it.
- **Soak**: the app left alone, sampled once a minute.
- **CPU per scene**: the scene picked in `family.json`, the app launched into Family Display (full screen on a 4K
  monitor), and the process's CPU time over 60 seconds.

## Findings

Measured on one desktop (a 24-core Intel desktop CPU, Windows 11, display at 150%). Frame times are from the Release
benchmark; app numbers are from a Debug build, which is fine for growth but not for absolute CPU.

### Every visited page stayed in memory (fixed)

Pages bind one-way into objects that live as long as the window: the photo backdrop (`ViewModel.Backdrop.Photo`) and the
kid picker (`ViewModel.KidPicker.HasSeveralKids`). x:Bind subscribes to their `PropertyChanged` and nothing ever
unsubscribed, so the backdrop and the picker held every page's view model, and for Doors the whole page with its 24
doors.

| After 200 churn rounds | Before | After |
| --- | --- | --- |
| Private bytes (started at ~260 MB) | 1,860 MB, still 1,360 MB after a full GC | 380 MB |
| Live `CalendarView` / `CalendarViewModel` | 203 / 203 | 0 / 0 |
| Live `CountdownViewModel` | 204 | 1 |
| Live `DisplayViewModel`, `WishListViewModel` | 203 each | 0 each |
| Live `DoorViewModel` | 4,872 | 0 |

Each view now calls `Bindings.StopTracking()` when it unloads. Any new view that x:Binds `OneWay` into something
window-scoped or app-wide needs the same.

### Replaced scenes kept their images until a full GC (fixed)

A scene caches its still parts as a window-sized image: 8 MB at 1080p, 33 MB at 4K. That memory is native, so the GC
doesn't see it and doesn't hurry. `SceneCanvas` replaced scenes without disposing them, and every page visit built a
new one. In the benchmark, 500 scene swaps without `Dispose` peaked between 0.8 and 2 GB at 1080p depending on when the
GC happened to run, and came back to 45 MB after a full GC. With disposal it stays at 54 MB throughout.

`SceneSlot` now owns the scene a canvas paints. It disposes the scene it replaces, and `SceneCanvas` empties it when the
canvas unloads and builds the scene again if it comes back. Some heads paint off the UI thread, so the slot takes a lock
around each frame and around the swap: a scene is never freed mid-frame.

### Northern lights did per-frame work it didn't need (fixed)

The aurora drew every ribbon as a column of rectangles 12 pixels wide, each with a gradient shader of its own, and
repainted its still hills, trees and cabin every frame. Now each ribbon is one mesh (`SKVertices`) drawn in a single
call, and the land is in the cached backdrop; only the cabin's windows are painted per frame. The picture is the same:
across 80 frames at four screen shapes, no channel differs by more than 3 of 255.

| Aurora | Before | After |
| --- | --- | --- |
| Windows app, Family Display at 4K, CPU | 37% of a core (3 runs: 36.5 to 37.0) | 8% (3 runs: 6.5 to 9.0) |
| CPU canvas, 1080p mean / p99 frame | 9.9 / 14.3 ms | 7.9 / 10.0 ms |
| CPU canvas, 4K mean / p99 frame | 40.0 / 46.7 ms | 34.8 / 40.2 ms |
| Managed allocation per frame | 128 KB (1080p), 215 KB (4K) | 4 KB |

An intermediate step, one shader per ribbon but still a rectangle per column, made no difference in the app (36 to 38%
in five runs); one draw per ribbon did. So on the GPU head the cost was the number of draws, about 1,000 a frame at 4K,
not the pixels. The other scenes cost 7 to 9% of a core in the same setup.

### Every scene, after the fixes

Mean frame time on a CPU canvas, Calm motion off (on is within noise of it, but runs at 10 fps instead of 30):

| | 1080p | 4K | Allocated per frame |
| --- | --- | --- | --- |
| Village | 2.2 ms | 8.1 ms | 11 KB |
| Snow globe | 2.3 ms | 9.1 ms | 3 KB |
| Northern lights | 7.9 ms | 34.8 ms | 4 KB |
| Fireplace | 3.4 ms | 14.2 ms | 3 KB |
| Toy train | 1.3 ms | 5.5 ms | 7 KB |
| Door confetti | 0.8 ms | 5.1 ms | 0.5 KB |
| Photo with the village in front | 2.3 ms | 8.0 ms | 8 KB |
| Photo alone | 0.6 ms | 3.3 ms | 0.2 KB |

The budget at 30 fps is 33 ms. Northern lights at 4K is still just over it on a CPU canvas: the ribbons are about 1.2
megapixels of additive gradient at 1080p, and on a CPU canvas filling them is the cost.

### Looked at, and fine

- **Window resizing**: 500 different sizes in a row, memory flat. Each size change frees the old backdrop.
- **Taps**: 10,000 taps, memory flat. Sparkles are capped at 300.
- **Photo slideshow**: 500 photo changes without disposing the old photo, the same memory as disposing it (70 to 75 MB).
  A photo is decoded lazily into Skia's own cache, which has a fixed budget (28 MB here).
- **Clocks**: the per-second ticker runs only while something listens, and every page unsubscribes on unload. After the
  churn there is one live view model per visible view, so one subscription each.
- **Steady state**: 10,000 frames of one scene, memory flat.
- **Sounds on Windows**: one AudioGraph and one node per effect, built once, rebuilt only if the device goes away.
  Read, not measured under load.
- **Door video**: there is no web view in the app. A video door opens the browser, behind the parent gate.

### Idle

Left alone after the fixes (Debug build, sampled once a minute): Home on the village for 35 minutes stayed between 210
and 216 MB private with handles and threads flat, at about 9% of one core. Family Display after that, 20 minutes,
stayed between 469 and 474 MB, at about 7% of one core.

## Not measured

- Android, iOS and WebAssembly, and their sound players (SoundPool, AVAudioPlayer, Web Audio). The leak fixed above is
  in shared code, so it was there too, but nobody has watched those heads' memory.
- The 12-hour (or month-long) run the issue asks for. The longest run here was 35 minutes on Home plus 20 in Family Display. A flat line over that
  is evidence, not proof, that a month is fine.
- GPU memory and GPU time on the Windows head, and frame times on a low-end device.
- A Release build of the app: app numbers are from Debug.
- Sticker and scene asset sizes against their displayed sizes, and photo import memory for a large phone photo.

## Worth doing next

- Northern lights at 4K on a CPU canvas (35 ms a frame, budget 33 ms): paint the ribbons at half resolution.
- Door confetti caches a fully transparent, window-sized backdrop (33 MB at 4K) and draws it every frame. It only shows
  for a moment, so it was left alone.
- `CompositionTarget.Rendering` stays subscribed while a scene is paused under the mini window, so XAML keeps ticking at
  the display's refresh rate. Cheap per tick, but not free; unmeasured.
