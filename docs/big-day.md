# The big day

The countdown runs to the family's big day. It is set in the grown-ups' corner, under **The big day**:

- **Christmas Eve** or **Christmas Day**. Each brings its usual hour: the evening for Christmas Eve, midnight for
  Christmas Day.
- **Another day**: any day of the year, picked as a month and a day. For families whose presents come at New Year, on
  6 January, on 6 December, or whenever they do. It comes round every year.
- **Exact time** (full version), in the section below, is the moment on that day the countdown reaches zero.

**Who brings the presents?** sits next to it. Both used to be on the Settings page; they moved here so a child
can't change them, and the grown-ups' corner is now the first entry in Settings.

## How it works

- `FamilySettings.BigDay` is `ChristmasEve`, `ChristmasDay` or `Custom`. For `Custom`, `CustomBigDayMonth` and
  `CustomBigDayDay` hold the date. `FamilySettings.BigDate()` gives the month and day whichever it is.
- `CountdownClock.Compute(now, month, day, arrivalMinutes)` counts to the next time that day comes round. 29 February
  falls on the 28th in other years.
- Celebrating lasts through the big day and the day after (Christmas Eve gets a third day, so both Christmases run
  to the 26th). A big day on 31 December is still being celebrated on 1 January.
- On the sign, a day of the family's own is named by its date (`Texts.BigDayName(settings)`).

The advent doors don't move: they are always 1 to 24 December.
