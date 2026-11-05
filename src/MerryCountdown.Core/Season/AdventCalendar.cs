namespace MerryCountdown.Core.Season;

public enum DoorState
{
    Locked,
    Ready,
    Opened,
}

public static class AdventCalendar
{
    public const int DoorCount = 24;

    private const int LastDayOfSeasonInJanuary = 6;

    /// <summary>The December a date belongs to. Last season's calendar stays up until Three Kings' Day.</summary>
    public static int SeasonYear(DateTime now) =>
        now.Month == 1 && now.Day <= LastDayOfSeasonInJanuary ? now.Year - 1 : now.Year;

    public static DoorState StateOf(int door, DateTime now, bool isOpened)
    {
        if (isOpened)
        {
            return DoorState.Opened;
        }

        return now.Date >= DoorDate(door, now) ? DoorState.Ready : DoorState.Locked;
    }

    public static int SleepsUntil(int door, DateTime now) =>
        Math.Max(0, (DoorDate(door, now) - now.Date).Days);

    public static int? TodaysDoor(DateTime now) =>
        now.Month == 12 && now.Day <= DoorCount ? now.Day : null;

    /// <summary>A shuffled wall of doors, because hunting for today's number is half the fun.</summary>
    public static IReadOnlyList<int> LayoutFor(int year)
    {
        var doors = Enumerable.Range(1, DoorCount).ToArray();

        // Stable across runtimes, unlike Random's unspecified sequence guarantees for seeded shuffles.
        var state = (uint)(year * 2654435761L);
        for (var i = doors.Length - 1; i > 0; i--)
        {
            state = (state * 1664525) + 1013904223;
            var j = (int)((state >> 8) % (uint)(i + 1));
            (doors[i], doors[j]) = (doors[j], doors[i]);
        }

        return doors;
    }

    private static DateTime DoorDate(int door, DateTime now) => new(SeasonYear(now), 12, door);
}
