# Scene taps

Tapping a scene should feel like poking something real. A tap on something the scene paints makes that thing react. A tap anywhere else throws the burst of stars. It's never both.

| Scene | Tap | Reaction | With Calm motion |
| --- | --- | --- | --- |
| Village | Tree | Lights grow and flash | Lights grow a little |
| Village | A house | Its windows blaze | Its windows glow |
| Snow globe | The glass | The snow whirls up | The snow stirs |
| Northern lights | The lights | They flare and reach higher | They brighten |
| Northern lights | The cabin | The lamps turn up | The lamps glow |
| Fireplace | The flames | They roar | The room glows |
| Fireplace | A stocking | It swings | It glows |
| Fireplace | A present | It hops | It glows |
| Toy train | The train | Cars bounce, steam billows, the lamp flashes | The lamp flashes |

Calm motion also slows the scene's clock, so every reaction plays out slower there. The door confetti (`ConfettiScene`) is a separate effect and doesn't take taps. The tap sound in [sound-effects.md](./sound-effects.md) plays for every tap, element or burst.

## Making something tappable

Override `ForegroundElements` in the scene, or `SkyElements` for anything a photo replaces, and return one `SceneElement` per thing:

```csharp
protected override IEnumerable<SceneElement> ForegroundElements(int width, int height, float time)
{
    var (x, ground, cabin) = Cabin(width, height);
    var halfRoof = cabin * 0.64f;
    yield return new SceneElement(new SKRect(x - halfRoof, ground - (cabin * 1.01f), x + halfRoof, ground), tappedAt => _cabinTapped = tappedAt);
}
```

- Get the area from the layout method the painting uses, here `Cabin`, so it stays right at every window size. A moving element works out its area from `time`, as `ToyTrainScene.Train` does.
- List elements front to back. The first one under the finger wins.
- `Scene` adds the margin for small fingers, so give it the painted size.
- In the callback, only store the time. Then use `Remaining(tappedAt, time, seconds)` in a `Paint...Life` method to get how much of the reaction is left. The result still depends only on the time passed in. Anything that moves has to be painted in `PaintForegroundLife`, because `PaintForeground` is cached, which is why the stockings and presents live there.
- Check `CalmMotion`, and light things up instead of moving them.

Photos are handled for you. A photo hides `SkyElements`, and a photo with no foreground hides `ForegroundElements` too, so those taps get the burst.

`SceneInteractionTests` asks every scene for its elements at several screen shapes, so a new element is covered as soon as it exists.
