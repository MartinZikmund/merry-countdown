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
`CalendarViewModel.DoorStartedSwinging`.

## What does the playing

`ISoundOutput` has one implementation per head, each called `PlatformSoundOutput` and each in that head's
`Platforms/` folder, so `App.RegisterServices` registers the same name everywhere. They all load every effect when the
app starts, which is what makes the first tap as quick as the tenth.

| Head | Plays through | Notes |
| --- | --- | --- |
| Windows (WinUI) | `AudioGraph`, one file input node per effect | The graph only runs around a sound and stops after four idle seconds. A running graph holds the audio device open and keeps the PC from sleeping. |
| Android | `SoundPool` | Usage `Game`, so it follows the media volume and doesn't take audio focus from music. |
| iOS | One prepared `AVAudioPlayer` per effect | Session category `Ambient`: it obeys the silent switch and plays over music. |
| WebAssembly | Web Audio, in `WasmScripts/MerrySounds.js` | Buffers are decoded up front. The browser keeps audio suspended until the first tap, and that tap resumes it. |
| Desktop (Skia) | Uno's `MediaPlayer`, one per effect | Windows, macOS and Linux share this build and have no sound API in common, so it's the only head that still needs the `MediaPlayerElement` Uno feature. |

A script added to `WasmScripts/` is not picked up by itself. Uno.Sdk only embeds `AppManifest.js`, so
`MerryCountdown.csproj` lists `MerrySounds.js` as an `EmbeddedResource`.

### Why not MediaPlayer everywhere

That's how it started, and taps felt late. Measured on the WinUI head, from the click to the first sample on Windows'
audio meter:

| | `MediaPlayer`, created on first use | `AudioGraph`, loaded at startup |
| --- | --- | --- |
| First play of an effect | 417 ms | 53 ms |
| The same effect again | 36 ms | 45 ms |
| Sticker pop | 183 ms | 57 ms |

Two things were slow, and neither was the player once it was warm. Each effect paid for opening its file the first
time it was heard, and some files began with silence: 130 ms of it before the sticker pop.

## Adding a sound

1. Add the value to `SoundEffect` and its file name to `SoundFiles.NameOf`.
2. Use MP3: every player above reads it, including the desktop head's. Mono at 96 kbps is plenty.
   Name it with lowercase letters and underscores only. Android packaging turns `-` into `_` while the player opens
   the original name, so a hyphenated sound fails on Android and nowhere else. `SoundFilesTests` guards this.
3. Cut the silence off the front, or the sound arrives late however fast the player is:

   ```bash
   ffmpeg -i in.wav -af "silenceremove=start_periods=1:start_threshold=-45dB:start_silence=0.005:detection=peak" -ac 1 -ar 44100 -c:a libmp3lame -b:a 96k out.mp3
   ```
4. Add the source and licence to the table below. If the author is new, add them to `SoundCreditsDescription` in both
   `Resources.resw` files.

## Credits

All sources are released under [CC0](https://creativecommons.org/publicdomain/zero/1.0/), so credit is optional. We give
it anyway, here and in **Settings > About**. The files were trimmed, mixed to mono and levelled with ffmpeg, and start
on their first audible sample.

| File | Source | Author |
| --- | --- | --- |
| `door_open.mp3` | `door_creak_open.wav` from [Various Sound Effects](https://opengameart.org/content/various-sound-effects), mixed with [Sleigh bells](https://commons.wikimedia.org/wiki/File:Sleigh_bells.wav) | laleksic, The Midnite Wolf |
| `door_locked.mp3` | [Knock on door](https://commons.wikimedia.org/wiki/File:Knock_on_door.wav) | Asud12 |
| `reveal.mp3` | `completetask_0.mp3` from [Completion sound](https://opengameart.org/content/completion-sound) | haeldb |
| `scene_tap.mp3` | `glass_004.ogg` from [Interface Sounds](https://opengameart.org/content/interface-sounds) | [Kenney](https://kenney.nl) |
| `punchline.mp3` | `fanfare_3.ogg` from [Classic fanfare lick](https://opengameart.org/content/classic-fanfare-lick) | fvcalderan |
| `sticker.mp3` | `pop1.ogg` from [3 pop sounds](https://opengameart.org/content/3-pop-sounds) | wubitog |
| `letter.mp3` | `whoosh2_0.wav` from [Air whoosh](https://opengameart.org/content/air-whoosh) | pyranostudios |
