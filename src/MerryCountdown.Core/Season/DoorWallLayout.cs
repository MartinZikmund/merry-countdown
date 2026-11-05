namespace MerryCountdown.Core.Season;

/// <summary>Picks whichever arrangement of 24 gives the biggest doors on this screen.</summary>
public readonly record struct DoorWallLayout(int Columns, double DoorWidth, double DoorHeight)
{
    public const double Spacing = 8;
    public const double SmallestDoorWidth = 72;
    public const double LargestDoorWidth = 170;

    private const double HeightPerWidth = 312d / 216;

    private static readonly int[] _columnChoices = [3, 4, 6, 8, 12];

    public static DoorWallLayout Fit(double width, double height)
    {
        var columns = _columnChoices[0];
        var door = 0d;
        foreach (var candidate in _columnChoices)
        {
            var rows = AdventCalendar.DoorCount / candidate;
            var fitWidth = FitWidth(width, candidate);
            var fitHeight = (height - ((rows - 1) * Spacing)) / rows;
            var fit = Math.Min(fitWidth, fitHeight / HeightPerWidth);
            if (fit > door)
            {
                door = fit;
                columns = candidate;
            }
        }

        return door >= SmallestDoorWidth ? Sized(columns, Math.Min(door, LargestDoorWidth)) : Scrolling(width);
    }

    // The whole wall can't fit, so fill the width with tappable doors and let it scroll.
    private static DoorWallLayout Scrolling(double width)
    {
        foreach (var candidate in _columnChoices.Reverse())
        {
            if (FitWidth(width, candidate) is var fit and >= SmallestDoorWidth)
            {
                return Sized(candidate, Math.Min(fit, LargestDoorWidth));
            }
        }

        return Sized(_columnChoices[0], SmallestDoorWidth);
    }

    private static double FitWidth(double width, int columns) => (width - ((columns - 1) * Spacing)) / columns;

    private static DoorWallLayout Sized(int columns, double doorWidth) => new(columns, doorWidth, doorWidth * HeightPerWidth);
}
