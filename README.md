# Merry Countdown

A joyful Christmas countdown for kids and families: how many sleeps are left, an advent calendar with a surprise behind
every door, and a letter to Santa or Ježíšek for every kid in the family.

Built with [Uno Platform](https://platform.uno/) 7 and plain WinUI XAML, from one codebase for **Android, iOS,
WebAssembly, Windows, macOS, Linux and WinUI**.

## What's inside

- **Countdown:** animated scenes you can tap, a huge sleep counter and a live clock.
- **Advent doors:** 24 shuffled doors with jokes, facts, missions, kind deeds and videos. Content rotates every year.
- **Wish lists:** every kid writes their own letter, or picks pictures if they can't write yet, then sends it off.
- **Family Display:** full screen for a TV or tablet left on all advent. Stays awake and drifts gently to protect the panel.
- **Grown-ups' corner:** behind a parent gate, because kids are smart but multiplication in words still works.

### Free and full version

| | Free | Full version |
| --- | --- | --- |
| Countdown, 24 doors, Family Display | ✓ | ✓ |
| A wish list and doors for every kid | ✓ | ✓ |
| Christmas Eve or Day, Santa or Ježíšek | ✓ | ✓ |
| Scenes | 2 | 5 |
| Your own photos as a slideshow, with an optional scene foreground | | ✓ |
| Personal messages behind doors | | ✓ |
| Countdowns to birthdays and family events | | ✓ |
| Exact time of the big moment | | ✓ |

The full version is one purchase on the Microsoft Store, Google Play and the App Store. There's no billing on web and
desktop, so everything is unlocked there.

**Privacy:** no accounts, ads or tracking. Everything stays on the device unless a grown-up shares it.

## Building

```bash
dotnet tool restore
dotnet build src/MerryCountdown/MerryCountdown.csproj -f net10.0-desktop
dotnet test tests/MerryCountdown.Core.Tests/MerryCountdown.Core.Tests.csproj
dotnet test tests/MerryCountdown.Scenes.Tests/MerryCountdown.Scenes.Tests.csproj
```

Other heads and prerequisites are in [docs/building.md](./docs/building.md). The product decisions are in the
[design](./docs/superpowers/specs/2026-11-05-merry-countdown-design.md), and what's needed before release is in
[store setup](./docs/store-setup.md).

To see December early in a debug build, set `MERRY_FAKE_DATE=2026-12-05`.

Family Display can start directly: `MerryCountdown --display` on desktop, or `?display=1` on the web.

## Credits

Built from [uno-app-template](https://github.com/MartinZikmund/uno-app-template). Fonts are
[Baloo 2](https://github.com/EkType/Baloo2) and [Patrick Hand](https://fonts.google.com/specimen/Patrick+Hand), both
under the SIL Open Font License (`src/MerryCountdown/Assets/Fonts`). The settings controls are a temporary source copy
from the [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows), MIT licensed.

## License

MIT. See [LICENSE](LICENSE).
