# Sound effects

The app answers taps with short sounds. There is no music or ambient loop, and nothing plays by itself, so a Family
Display left on the TV all advent stays silent until someone taps it.

| When | `SoundEffect` | File in `src/MerryCountdown/Assets/Sounds/` |
| --- | --- | --- |
| A new door opens: as it starts to swing, or straight away with Calm motion | `DoorOpen` | `door_open.mp3` |
| A locked door is tapped | `DoorLocked` | `door_locked.mp3` |
| An already opened door is opened again | `Reveal` | `reveal.mp3` |
| The scene is tapped | `SceneTap` | `scene_tap.mp3` |
| A joke's punchline is shown | `Punchline` | `punchline.mp3` |
| A wish sticker is picked | `Sticker` | `sticker.mp3` |
| The wish letter is sent | `Letter` | `letter.mp3` |

Grown-ups can switch sounds off under **Settings > Sounds**. Calm motion doesn't silence them.

## Playing a sound

View models take `ISoundService` and call `Play`, which checks the setting:

```csharp
_sounds.Play(SoundEffect.Reveal);
```

Taps that only a view can see, such as a door starting to swing, reach the view model through a small method like
`CalendarViewModel.DoorStartedSwinging`. `MediaSoundOutput` in the head does the playing, with one `MediaPlayer` per
effect, so different sounds overlap and a repeated one restarts.

## Adding a sound

1. Add the value to `SoundEffect` and its file name to `SoundFiles.NameOf`.
2. Use MP3: it's the one format Uno's media player supports on every target. Mono at 96 kbps is plenty.
   Name it with lowercase letters and underscores only. Android packaging turns `-` into `_` while Uno's player opens
   the original name, so a hyphenated sound fails on Android and nowhere else. `SoundFilesTests` guards this.
3. Add the source and licence to the table below. If the author is new, add them to `SoundCreditsDescription` in both
   `Resources.resw` files.

## Credits

All sources are released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/), so credit is optional. We give
it anyway, here and in **Settings > About**. The files were trimmed, mixed to mono and levelled with ffmpeg.

| File | Source | Author |
| --- | --- | --- |
| `door_open.mp3` | `door_creak_open.wav` from [Various Sound Effects](https://opengameart.org/content/various-sound-effects), mixed with [Sleigh bells](https://commons.wikimedia.org/wiki/File:Sleigh_bells.wav) | laleksic, The Midnite Wolf |
| `door_locked.mp3` | [Knock on door](https://commons.wikimedia.org/wiki/File:Knock_on_door.wav) | Asud12 |
| `reveal.mp3` | `completetask_0.mp3` from [Completion sound](https://opengameart.org/content/completion-sound) | haeldb |
| `scene_tap.mp3` | `glass_004.ogg` from [Interface Sounds](https://opengameart.org/content/interface-sounds) | [Kenney](https://kenney.nl) |
| `punchline.mp3` | `fanfare_3.ogg` from [Classic fanfare lick](https://opengameart.org/content/classic-fanfare-lick) | fvcalderan |
| `sticker.mp3` | `pop1.ogg` from [3 pop sounds](https://opengameart.org/content/3-pop-sounds) | wubitog |
| `letter.mp3` | `whoosh2_0.wav` from [Air whoosh](https://opengameart.org/content/air-whoosh) | pyranostudios |
