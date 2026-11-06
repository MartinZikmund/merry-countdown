# The countdown sign

To change how the home screen counts down, open the palette button at the top right of the scene. Families pick a **sign** (snowy plank, gingerbread, parcel tag, station sign or bauble garland) and a **countdown style** (number plates or sleeps and a clock). Family Display follows the same two settings.

Both are saved in `FamilySettings.Sign` and `FamilySettings.CountdownStyle`. Number plates on the snowy plank are the default, and families who saved data before signs existed get that default too.

## How it fits together

- `CountdownSignViewModel` holds everything the sign shows. `CountdownViewModel` and `DisplayViewModel` each own one and call `Update` every second.
- `CountdownSign` draws it. All four board designs share one layout and only swap their surface and colours; the bauble garland has its own layout, and `Garland` draws the pine band across the top of the page.
- `DigitText` draws numbers in fixed cells, because Baloo's "1" is much narrower than its "0" and plain centred text would shuffle sideways on every tick.

## Sleeps and the clock

`CountdownState.UntilNextSleep` counts to the moment `Sleeps` next changes: midnight, or the arrival time once no sleeps are left. It isn't `Remaining`, which always counts to the arrival, so a Christmas Eve family arriving at 17:00 would otherwise see "15 sleeps" next to "14 days".

## Adding a sign

1. Add a value to `SignDesign` and a `Sign_<value>` string to both `Resources.resw` files.
2. Add its colours and padding to `SignLook` in `CountdownSign.xaml.cs`.
3. Add its surface to `CountdownSign.xaml`. Anything drawn with a `Path` goes in one of the `Canvas` layers, so it can't change the size of the board: a `Path` measures to its geometry, and following the board's size from there is a layout cycle.
