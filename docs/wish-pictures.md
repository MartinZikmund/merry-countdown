# Wish pictures

A kid who can't write yet asks for things by picking a picture. The Wishes page shows one button, "Pick a picture",
that opens a flyout with all of them, grouped by kind and scrollable. Picking one adds the wish straight away; see
[the wish letter](./wish-letter.md).

## Adding a picture

1. Draw `src/MerryCountdown/Assets/Stickers/<key>.svg`: 96 x 96, flat colors, the navy `#1B2440` for details,
   no gradients. Look at `teddy.svg` or `kite.svg` for the style. Uno turns it into the PNG the app loads.
2. Add the key to a group in `StickerCatalog` (`src/MerryCountdown.Core/Family/StickerCatalog.cs`).
3. Add `Sticker_<key>` to both `Strings/en/Resources.resw` and `Strings/cs/Resources.resw`. Screen readers
   and the shared text list read it.

`StickerCatalogTests` fails if a key has no SVG or no name in either language.

A group is a new `StickerGroup("key", [...])` plus `StickerGroup_<key>` in both resw files.

**Never rename or remove a key.** A saved wish stores it.

## The pieces

- `StickerCatalog` (Core): the groups and keys, in the order the flyout shows them.
- `WishListViewModel.StickerGroups`: the same groups with localized names; `Stickers` is the flat list.
  Picking runs `ChoosePictureCommand`, which sets `SelectedSticker`; ＋ puts it on the letter.
- `Controls/StickerPicker`: a `UserControl` with three properties: `Groups`, `SelectedSticker` and `ChooseCommand`.
  It knows nothing about wishes, so another page can reuse it. The Wishes page binds `SelectedSticker` to the
  picture waiting for ＋, so the button shows it.

Each picture is a 64 x 64 target at least; the flyout is at most 336 wide and shrinks to fit a 320-wide phone
(three per row there).
