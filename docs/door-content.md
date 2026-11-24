# Door content

What is behind the doors lives in `src/MerryCountdown.Core/Content/doors-en.json` and `doors-cs.json`. `DoorLibrary` picks from it; free and full-version doors use the same files. Each kind has its own pool, a year rotates through it in steps of 3, and door 24 is always the finale.

## Adding or changing an item

- Write both languages. The pools must have the same size in `en` and `cs` (a test checks).
- The Czech file is its own set, not a translation. A pun that only works in English gets a Czech pun, not a literal version.
- `{bringer}` in a text becomes Santa or Ježíšek, whichever the family picked. Never write either name straight into a text, joke or fact.
- The sticker must show what the text is about, and the file must exist in `Assets/Stickers` (a test checks).
- Door text stays under 125 characters, a punchline under 45, a Family Display fact under 80 (a test checks).
- Nothing to buy, nothing outdoors, nothing that needs a sibling or a grandparent.
- A fact needs a source you actually looked at. "Everyone knows" is how "no two snowflakes are alike" got in.

## Czech typography

A line must not end on a one-letter word (`k s v z o u a i`) or on a day number like `4.`. Put a non-breaking space after it: ` ` in the JSON. A test fails on any ordinary space there.

## Videos

Videos are YouTube ids, opened outside the app behind the parent gate. Check `https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v=<id>&format=json` returns 200, then a person watches the whole video. Links rot.
