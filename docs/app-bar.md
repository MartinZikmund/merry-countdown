# The app bar

To add a section, add a `RadioButton` to `TabPanel` in `WindowShell.xaml` with `AppBarTabStyle`, a sticker and a label, set its `Tag` to the `NavigationSection` name, and add it to the list in `WindowShell.UpdateTabSelection`.

- The tabs are radio buttons with their own template, not a `NavigationView`, so they can look like the rest of the app: stickers for kids who can't read yet, Baloo for the label, and the same gold as the app's other big buttons for the selected tab.
- Below 480 wide, the label moves under the sticker so three tabs and the settings button fit on a small phone.
- Settings is the same kind of tab with a plain icon and no label, because it's for grown-ups.
- A back button appears at the start of the bar when there is somewhere to go back to.
- On Home, a palette button sits next to Settings. It opens the [look picker](#the-customize-button).

## The door badge

Home doesn't show a door, so the scene stays clear. Instead, `WindowShellViewModel.WaitingDoorNumber` puts today's door number on the Doors sticker until the active kid opens that door. There's no badge outside December 1 to 24. It updates when the family data changes, when the active kid changes, and every second, so it reappears as soon as a new day starts.

This is the only notification the app has. It stays inside the app, so it needs no platform permissions.

## The title bar on Windows

On the WinUI head the bar is the title bar: the window extends into it, and the tabs share a row with the minimize, maximize and close buttons. The Uno heads keep their own window chrome, so nothing changes there.

- `DragRegion` is the empty stretch between the tabs and the buttons on the right. It's the only part handed to `Window.SetTitleBar`, so it's the only part that drags the window. A drag region swallows clicks, which is why it never overlaps a control.
- `LeftInsetColumn` and `RightInsetColumn` take their widths from `AppWindow.TitleBar.LeftInset` and `RightInset`, converted from pixels to DIPs. They keep Settings out from under the caption buttons, on whichever side a right-to-left layout puts them.
- When the window is too narrow to leave 64 DIPs to drag by, the tabs drop to their own row under a slim strip with the app's icon and name, and the strip becomes the drag region. `AppBarLayout.SharesRowWithCaptionButtons` in Core makes that call, and `WindowShell.ArrangeTitleBar` applies it.
- The [compact overlay](./compact-overlay.md) collapses the bar, so it has a drag strip of its own, `OverlayDragRegion`.

To add something to the bar, give it its own column and add its width to `content` in `ArrangeTitleBar`, or the tabs will crowd out the drag region before they move down.

## The customize button

Home used to float three round buttons over the scene: the palette, the mini window and Family Display. They now live behind one palette button in the bar, shown only on Home. Its flyout is `Controls/LookPicker`: the two display modes with their names spelled out, then the countdown style, the sign and the scene.

`WindowShell.OfferCustomizing` builds a new `LookPicker` on each visit to Home, because every visit gets its own `CountdownViewModel`. The picker raises `CloseRequested` for choices that take over the window or open a dialog. Picking a sign or a style keeps it open, so you can watch the sign change behind it.
