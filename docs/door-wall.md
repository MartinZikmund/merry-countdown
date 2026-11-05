# The door wall

To change how a door looks or opens, edit `DoorArt`. It draws one door and owns its swing and shake, and both the Doors page (`DoorTile`) and today's door on Home (`HomeDoor`) use it.

## What each door shows

| State | How it looks |
| --- | --- |
| Locked | Its colour faded halfway to a quiet tone, with a big padlock. Nothing is behind it yet, so its surprise can't peek out. |
| Ready | Full colour with a knob. Today's door also gets a gold frame and a gentle pulse. |
| Opened | Stays swung open with its sticker showing. |

`DoorViewModel.Tint` gives each door its colour from the `DoorTint{n}Brush` resources.

## Opening a door

The leaf sits in front of the frame and turns on a `PlaneProjection` hinged at its left edge, so it comes out of the doorway towards the viewer, with its free edge growing taller. Open doors overhang their cell, which is why the wall has a margin inside its `ScrollViewer` and open doors draw over their neighbours. It has to be a margin on the wall: the scroller clips to the inside of its own padding, so padding would cut off open and pulsing doors.

`DoorTile` plays the swing first and raises `DoorChosen` when it ends, and only then does `CalendarViewModel.TapDoor` mark the door opened and show its reveal. `DoorViewModel.ShouldSwingOpen` decides whether there is a swing at all: with **Calm motion** on, a ready door opens straight away and nothing pulses or shakes.

## Fitting the wall

`DoorWallLayout.Fit` tries 3, 4, 6, 8 and 12 columns and keeps whichever gives the biggest doors with the whole wall on screen. If nothing fits at a tappable size, it fills the width and lets the wall scroll.

To try it in December, debug builds read a fake date from `MERRY_FAKE_DATE`, or from a `devdate.txt` in the app's local folder when running packaged.
