# The wish letter

The Wishes page is a letter to Santa or Ježíšek. A kid builds it, then sends it. Both steps have to work for a
kid who can't read yet, on a 320-wide phone.

## Building: the add bar

Under 840 wide, the tools are a bar pinned to the bottom of the page, above the [bottom tabs](./app-bar.md#bottom-tabs-on-phones).
Only the letter scrolls, so the way to add a wish is always in reach however long the letter gets. When wide, the
same tools are a column next to the letter that doesn't scroll.

- **Pick a picture** comes first and is the biggest target. Picking one adds the wish at once
  (`AddPictureWishCommand`). If there are words in the text box, the picture takes them along.
- **Or write a wish**: the text box and its round ＋ button (or Enter) add a typed wish (`AddWishCommand`).
- **An empty letter** shows a dashed "Your first wish goes here" slot. Tapping it opens the pictures.
- **A new wish** is scrolled to the middle of the letter, pops in and glows (`ArrivedWish`). With Calm motion
  it jumps there without scrolling smoothly and without the pop, and still glows.
- **Removing** is a 48 x 48 round button on each line. The bar then says what went and offers **Put it back**
  (`UndoRemoveCommand`), which restores the wish where it was. The offer lasts until the next add, send or kid switch.

## Sending

Sending lives after the signature: at the end of the letter on a phone, at the bottom of the tools column when wide.
A line above **Send my letter!** says what's missing (`SendHint`): a wish first, then a name. The letter needs both,
and a new kid has no name, so the hint says a grown-up can help. The button stays disabled until then.

## The on-screen keyboard

Android runs with `AdjustNothing`, so the system doesn't move anything when the keyboard opens. On Android and iOS
the page listens to `InputPane.Showing`/`Hiding` and lifts itself by the part of the keyboard that covers the page
(the keyboard covers the tab bar first). While the keyboard is up on a phone, the picture button folds away, so the
text box and ＋ stay above the keyboard with some of the letter still showing.

This has not been run on a phone. Check there: the text box and ＋ sit just above the keyboard on a 320-wide phone,
nothing is left lifted after the keyboard closes, and the bar doesn't jump twice.

## The pieces

- `WishListViewModel` (Core): the commands above, `ArrivedWish`, `CanUndoRemove`/`RemovedMessage`, `SendHint`,
  `CanSend`, `IsEmpty` and `CalmMotion`. `WishLetterFlowTests` covers them.
- `IFamilyService.RestoreWish` puts a removed wish back at its old position.
- `Views/WishListView`: the layout and the movement. A visual state can't move an element to another panel, so
  `ApplyLayout` moves the send section between the tools and the end of the letter.
- `Controls/StickerPicker`: see [wish pictures](./wish-pictures.md). `Open()` opens its flyout from elsewhere.
