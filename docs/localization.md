# Localization

How to add or change a string, and what Czech needs that English does not. Strings live in
`src/MerryCountdown/Strings/{en,cs}/Resources.resw`; every key exists in both.

## Counted things

Put a count next to a noun with `IStringLocalizer.Plural` (`MerryCountdown.Core.Infrastructure.Texts`):

```csharp
var label = localizer.Plural("UnitDays", days);   // UnitDays_One / _Few / _Many
```

Add all three keys to both languages. The form is `_One` for 1, `_Few` for 2 to 4, `_Many` for everything else
(including 0). English repeats its plural in `_Few` and `_Many`. `ResourceStringsTests` fails if a key is
missing one of the three.

## Names

Czech declines names, and the app does not try to. Two ways out:

- Keep the name in the nominative: after a colon (`Odebrat: {0}?`) or as a label.
- If the name must sit inside a sentence and there are only a few of them, write one whole sentence per name.
  The wish letter does this for the gift-bringer: `LetterDear_*` and `LetterSent_*`, picked by
  `GiftBringer`.

## Czech house style

- Kids' text uses tykání and no gender: no `přál(a)`, `zapomněl(a)`. Grown-ups' text uses vykání.
- The same word for the same thing: *okénko*, *přání*, *dopis*, *nastavení*, *spaní*.
- A non-breaking space (`&#160;`) after `k`, `s`, `v`, `z`, `o`, `u`, `a`, `i`, and between a number and its noun.
  `ResourceStringsTests` checks the one-letter words.
- Czech quotes are `„ “`, never `"`.
