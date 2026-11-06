# The app bar

To add a section, add a `RadioButton` to `TabPanel` in `WindowShell.xaml` with `AppBarTabStyle`, a sticker and a label, set its `Tag` to the `NavigationSection` name, and add it to the list in `WindowShell.UpdateTabSelection`.

- The tabs are radio buttons with their own template, not a `NavigationView`, so they can look like the rest of the app: stickers for kids who can't read yet, Baloo for the label, and the same gold as the app's other big buttons for the selected tab.
- Below 480 wide, the label moves under the sticker so three tabs and the settings button fit on a small phone.
- Settings is the same kind of tab with a plain icon and no label, because it's for grown-ups.
- Uno heads get a back button in the bar when there is somewhere to go back to. On Windows, the title bar has it.

## The door badge

Home doesn't show a door, so the scene stays clear. Instead, `WindowShellViewModel.WaitingDoorNumber` puts today's door number on the Doors sticker until the active kid opens that door. There's no badge outside December 1 to 24. It updates when the family data changes, when the active kid changes, and every second, so it reappears as soon as a new day starts.

This is the only notification the app has. It stays inside the app, so it needs no platform permissions.

## Why the scene controls sit in the sky

Scenes keep their tappable things low down ([scene-taps.md](./scene-taps.md)), so the palette and Family Display buttons stay at the top. `CountdownView.ArrangeForSize` measures the sign and puts them beside it if they fit, or under it if they don't.
