# Dialogs

To ask or tell the user something from a view model, take `ISnackService` (`ShowMessageAsync`, `ConfirmAsync`) or
`IParentGateService`. All of them end up as a `ContentDialog`, and every `ContentDialog` in the app looks the same
without being asked to: `Resources/Dialogs.xaml` holds an implicit style, so a dialog built in code gets it too.

## The look

- One rounded card with a thin gold edge: cream in the light theme, night-sky navy in the dark one
  (`DialogBackgroundBrush`, `DialogBorderBrush` in `Colors.xaml`). The stock dialog's separate footer band is gone.
- The app's own `ScrimBrush` behind it, in place of Fluent's grey smoke.
- The title in Baloo, like page titles. Body text at 16.
- Pill buttons in Baloo, 44 high so a small finger can hit them. The default button is the accent pill, the same as
  `BigActionButtonStyle` elsewhere.

## Why the template is a copy

`MerryContentDialogStyle` carries WinUI's whole `ContentDialog` template with those changes made in place. Fluent's
lightweight styling would have been less to own, but the stock template swaps the default button's style to
`AccentButtonStyle` by name, from a visual state. No style setter and no resource override reaches that, so the default
button would have stayed square and in Segoe while its neighbour turned into a pill.

The template is the one in `microsoft-ui-xaml/controls/dev/CommonStyles/ContentDialog_themeresources.xaml`, which Uno's
Fluent theme mirrors line for line. When either changes, diff against that file. The edits are small: the smoke fill,
the corner radius on `DialogSpace`, the paddings, the title's face and size, and the three `…AsDefaultButton` states.

## Following the theme

A dialog lives in the popup layer, outside `WindowShell`, so it doesn't inherit the theme chosen in **Settings**.
`DialogCoordinator` copies the shell's `RequestedTheme` onto each dialog before showing it. Show dialogs through
`IDialogService`, `IConfirmationDialogService` or the services above, never with `ContentDialog.ShowAsync` directly:
the coordinator is also what queues them, since WinUI throws when a second dialog opens over the first.

## A dialog with its own content

Build the `ContentDialog` and hand it to `IDialogService.ShowAsync(ContentDialog)`, as `ParentGateService` does for
the grown-ups question. Set `DefaultButton` so one button gets the accent pill and the Enter key.
