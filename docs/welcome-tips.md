# Welcome tips

A new family gets two tips, once, so the palette and the cog in the app bar don't go unnoticed:

1. **Make it yours** points at the Customize button: scenes, signs, photos and the big screen.
2. **For the grown-ups** points at Settings.

## When they show

- A moment after Home first appears, so the scene is seen first.
- Only while the app bar is in view: not in the Family Display or the mini window.
- Once per device. Finishing the second tip, or closing either one, sets `IAppPreferences.HasSeenWelcomeTips`.
  Quitting the app with a tip still open does not, so the tips come back next time.
- Leaving Home during the first tip moves on to the second, because the Customize button is only on Home.

## How it is built

- `WelcomeTips` in `MerryCountdown.Core/Services` holds which tip is current and when the tour is over. It is covered by
  `WelcomeTipsTests`.
- `WindowShell` owns the two `TeachingTip`s and opens whichever one `WelcomeTips.Current` names.
- The look comes from `MerryTeachingTipStyle` (buttons) and the `TeachingTip*` brushes in `Colors.xaml` (card), which
  reuse the dialogs' colours. See [dialogs.md](./dialogs.md).

## Adding a tip

Add a value to `WelcomeTip`, its step in `WelcomeTips.Next`, a `TeachingTip` in `WindowShell.xaml` and its strings in
both languages. To see the tips again on a device that has seen them, clear the app's settings.
