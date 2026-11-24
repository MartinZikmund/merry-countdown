# Scene shake

Shaking the phone, or grabbing the window by its title bar and shaking it, makes the scene on Home and in the mini window react. The reaction follows the shake while it lasts and settles a couple of seconds after it stops. A harder shake reacts more.

| Scene | Shaken | With Calm motion |
| --- | --- | --- |
| Snow globe | The snow whirls up | The snow stirs |
| Village | Snow slides off the roofs and the tree, the tree's lights jingle | The tree's lights grow, the windows glow softly |
| Northern lights | The ribbons flare and reach higher, the cabin lamps turn up | The ribbons brighten, the lamps glow |
| Fireplace | Stockings swing, presents hop, sparks fly, the fire roars | The stockings, presents and room glow |
| Toy train | Cars bounce, steam billows, the lamp flashes | The lamp flashes |
| A photo with no foreground | A burst of snow | A slower, fainter burst |

The mini window always draws its scene with Calm motion, so shaking it gives the calm column. Family Display doesn't listen: it runs on a TV.

## Where each piece lives

- **Scenes** (`MerryCountdown.Scenes`): `Scene.Shake(strength, time)` is the second input next to `Tap`. Call it for every push of the shake. A scene reads what's left with `Shaken(time, seconds)`, the strength fading to 0 the given time after the last push, and uses it in its `Paint...Life` methods the same way it uses `Remaining` for a tap. A tapped element and a shake share the same reaction where they can, taking whichever is stronger.
- **Detection** (`MerryCountdown.Core/Shaking`): `ShakeDetector` turns pushes into shakes. Four quick pushes, each back the way the last one came, make a shake; anything slower, weaker or in one direction doesn't. `WindowShakeTracker` feeds it window positions in DIPs, `MotionShakeTracker` accelerometer readings in g.
- **Service** (`IShakeService` in Core): raises `Shaken` on the UI thread for every push. It only listens to the hardware while something is subscribed, and the view models subscribe only while Home or the mini window is showing.
  - Windows and Skia desktop: `WindowShakeService` follows `AppWindow.Changed` with `DidPositionChange`.
  - Android and iOS: `MotionShakeService` reads `Windows.Devices.Sensors.Accelerometer`. Neither needs a permission.
  - Web: `NoShakeService`. iOS Safari asks before it shares motion, so the web head is left out for now.
- **View** (`SceneCanvas.Shake`): the view models expose the latest push as `Shake`, and the canvas passes each new one to the scene at its own clock.

## Adding a shake reaction to a scene

```csharp
var shaken = Shaken(time, ShakeSettle);
var flare = Math.Max(Remaining(_lightsTapped, time, LightsReaction), shaken);
```

- Scale the reaction by what `Shaken` returns, so a gentle shake stays gentle.
- Anything that should keep moving for as long as the shake lasts takes its phase from `time`, not from the moment of the shake.
- Check `CalmMotion`, and light things up instead of moving them.

`SceneShakeTests` checks every scene: a shaken frame differs from a settled one, the reaction lasts while the shake does and is gone afterwards, a harder shake reacts more, and Calm motion answers more gently. `ShakeDetectorTests` holds the recorded-looking sample sequences for what is and isn't a shake. See [scene-taps.md](./scene-taps.md) for the tap side.
