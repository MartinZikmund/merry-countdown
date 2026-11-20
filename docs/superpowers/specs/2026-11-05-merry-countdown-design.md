# Merry Countdown: design

A joyful Christmas countdown for kids and families: a much improved, modern take on the 2021
[ChristmasCountdown](https://github.com/MartinZikmund/ChristmasCountdown) app, built on
`uno-app-template` and Uno Platform 7.0 dev bits.

## Decided by Martin

- **Separate app.** Not related to *Lumo's Village* (`D:\Personal\christmas-countdown`), which keeps its
  repo, name and identity. No game engine here: the countdown is the star.
- **Door content:** built-in surprises plus curated videos.
- **Paid full version:** extra scenes and family personalisation.
- **Web and desktop:** no billing, so everything is free there.
- **Wish list:** kids write to Santa or Ježíšek. Every kid has their own list.
- **Family Display:** a full-screen mode to leave running on a TV or tablet all advent.
- **Platforms:** Android, iOS, WebAssembly, Desktop (Windows, macOS, Linux) and WinUI.

## Calls made while building (easy to change)

| Call | Why |
| --- | --- |
| Working title **Merry Countdown** (`dev.mzikmund.merrycountdown`) | *Christmas Countdown* and its id belong to Lumo's Village. |
| **Kid profiles are free** | "Each kid has their own list" shouldn't sit behind a paywall. The paid tier keeps personal door messages, family events and exact arrival time. To gate it, check `IFullVersionService.IsFull` in `ParentZoneViewModel.AddKid`. |
| Eve/Day choice and Santa/Ježíšek are free, defaulted from the culture | Czech families shouldn't pay to get the right day. Only the exact time is paid. |
| 2 free scenes (village, snow globe), 3 paid (northern lights, fireplace, toy train) | A sleigh scene was dropped: it wouldn't fit Ježíšek families. |
| One non-consumable product, `dev.mzikmund.merrycountdown.pro`, about €3.99 | No subscription for a seasonal family app. The price is set in the store consoles. |
| Parent gate: multiplication in words | Purchases and anything leaving the app (videos, sharing) sit behind it, as kids' store policies expect. |
| Videos open in the browser, no thumbnails | No network calls, no embedded player, no tracking inside the app. |
| Sound effects on taps only, no music | See [sound-effects.md](../../sound-effects.md). A TV left on all advent stays silent, and every source is CC0. |
| No notifications | Not requested, and they need platform permissions in a kids' app. |

## Product shape

- **Countdown:** full-bleed animated scene, a huge "N sleeps", a live clock, family events. A badge on the Doors tab says when today's door is waiting ([app-bar.md](../../app-bar.md)). Tap something in the scene and it reacts; tap anything else for sparkles. See [scene-taps.md](../../scene-taps.md).
- **Doors:** 24 shuffled doors that lay themselves out to fill any screen. Locked doors jiggle and say how many sleeps are left; missed days stay open. Behind them: jokes with hidden punchlines, facts, missions, kind deeds and videos, rotating each year.
- **Wishes:** a letter on paper in a handwritten font. Write a wish or pick a picture (for kids who can't write yet), sign it, send it, watch it fly. "I forgot something!" reopens it.
- **Family Display:** the current scene, huge number, date and clock, one card per kid and a fact of the day. Screen kept awake, content drifts slowly to protect panels, scenes rotate every 30 minutes. `--display` (desktop) or `?display=1` (web) starts straight in it.
- **Compact overlay:** the sign on the scene in a small always-on-top window, on the WinUI head. See [compact-overlay.md](../../compact-overlay.md).
- **Grown-ups' corner:** behind the parent gate. Kids, the full version, messages behind doors, family events, exact time.

## Custom photos

Decided by Martin: photos fill the screen behind the countdown, the scene's foreground can optionally stay in front,
it's part of the full version, and several photos run as a slideshow at a chosen interval.

My calls: photos are added and deleted in the grown-ups' corner, while kids can only switch to them from the scene menu.
The foreground is a choice of scene rather than an on/off switch. Intervals run from 30 seconds to an hour, 5 minutes by
default, with a crossfade. In Family Display the album replaces scene rotation and the arrow keys move through photos.
Each photo is copied into app storage upright, at most 2560 pixels on its long side, as JPEG. Refunding the full version
hides the photos but doesn't delete them.

## Structure

```
src/MerryCountdown.Scenes   scenes in pure SkiaSharp, no Uno, tested headlessly
src/MerryCountdown.Core     view models, season logic, family data, store logic
src/MerryCountdown          head: views, canvas host, platform services
```

Everything is stored as `family.json` in the app's local folder. There are no accounts and no network calls, except
the optional store billing and a video a grown-up chooses to open.
