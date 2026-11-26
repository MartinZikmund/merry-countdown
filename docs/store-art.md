# Store art

How the store screenshots and key art are made, so the next update (or the next store) gets the same look. Everything
lives in `store-art/`: the templates are tracked, the captures (`raw*/`) and renders (`out*/`) are git-ignored.

## What's in the folder

| File | Makes |
| --- | --- |
| `compose.html` | Landscape screenshot frames, 3840 × 2160 (Microsoft Store, Play tablets). `?f=1..8&lang=en|cs`; for other captures `&dir=raw-tablet&tag=N` (the number on the door tag) and `&crop=x,y,w,h&rw=<capture width>&zoom=` for frame 3's zoomed door card. |
| `compose-phone.html` | Portrait screenshot frames, 2160 × 3840 (Play phones), from `raw-phone/`. `?f=1..6&lang=en|cs`. |
| `art.html` | Key art. `?kind=hero|poster|box|square|titled|feature`, optional `&sub=` subtitle and `&zoom=` to render a big layout at a smaller size. |
| `tile.html` | The app icon tile at any size. `?s=512`. |
| `seed/family-*.json` | The demo family (Emma and Leo / Ema and Kuba) for captures. |
| `render.py` | Renders a whole set: `python store-art/render.py play` or `msstore`. |

The templates use the app's own fonts, stickers and icon from `src/MerryCountdown/Assets/`, so the art follows the app.
The copy on each frame is in the template's `copy` object, in English and Czech.

## Rendering

```bash
python store-art/render.py play       # out-play/: icon, feature graphics, 6 phone + 7 tablet frames per language
python store-art/render.py msstore    # out-msstore/: tiles, key art, 8 frames per language
```

`render.py` finds Chrome on Windows and macOS, or takes `CHROME=<path>`. It runs headless Chrome at the exact output size.
The `play` target reproduces the set uploaded to Google Play on 2026-11-26 pixel for pixel. The `msstore` target's key
art uses the templates' defaults; the Microsoft Store screenshots were framed from `raw/` captures of the Windows app.

## Capturing

Captures go to `store-art/raw/` (Windows), `raw-phone/` and `raw-tablet/` (Android), named `<lang>-<frame>.png`:
`1-sleeps`, `2-doors`, `3-door`, `4-wishes`, `5-display`, `scene-aurora`, `scene-fireplace`, `scene-train`.

1. Build a Debug head with `-p:AppChannel=Prod -p:UnoDisableHotDesign=true`, in a throwaway worktree where
   `MainWindow.UseStudio()` in `App.xaml.cs` is commented out (it doesn't compile without Hot Design). Prod hides the DEV
   badge; Debug keeps the hooks below.
2. Seed the app's `LocalFolder` (on Android `files/`, via `adb push` to `/data/local/tmp` and `run-as <package> cp`):
   `family.json` from `seed/`, `devlang.txt` (`en-US` or `cs-CZ`, read by a Debug-only hook in `App`), `devdate.txt`
   (`2026-12-12`: doors 1–11 open, door 12 waiting) and `simulate-store.txt` containing `owned` (Pro unlocked). Clear the
   app's data between languages.
3. Dark theme on the device, then dismiss the welcome tip and pick the "Sleeps and a clock" sign in the look picker.
4. Clean status bar. Android: `settings put global sysui_demo_allowed 1`, then `am broadcast -a com.android.systemui.demo`
   with `-e command clock -e hhmm 0900`, `battery -e level 100`, `network -e mobile hide`,
   `network -e wifi show -e level 4 -e fully true` and `notifications -e visible false`. iOS Simulator:
   `xcrun simctl status_bar booted override --time 9:41 --batteryState charged --batteryLevel 100`.
5. The toy train moves: take a burst of screenshots and keep the one with the train in the middle.

Android captures used dedicated emulators (Pixel 9 Pro 1280 × 2856 and Pixel Tablet 2560 × 1600, 8 GB data partition)
and an x64-only APK (`-p:RuntimeIdentifier=android-x64`), because a full Debug APK doesn't fit a default emulator.

## Store sizes

| Store | Asset | Size |
| --- | --- | --- |
| Google Play | icon / feature graphic | 512 × 512 / 1024 × 500 |
| Google Play | phone / tablet screenshots | 2160 × 3840 / 3840 × 2160 |
| Microsoft Store | screenshots, hero | 3840 × 2160 |
| Microsoft Store | poster / box art | 1440 × 2160 / 2160 × 2160 |

The listing text that goes with them is in [store-listing.md](./store-listing.md).
