namespace MerryCountdown.Core.Season;

public enum DoorState
{
    Locked,
    Ready,
    Opened,

    /// <summary>Door 1 before advent starts: it can be looked behind, but it isn't opened until its day.</summary>
    SneakPeek,
}

/// <summary>One year's 24 doors, a door a day, the last one on <paramref name="LastDoor"/>.</summary>
public readonly record struct AdventSeason(DateTime LastDoor)
{
    /// <summary>What this season's opened doors and letter are filed under.</summary>
    public int Year => LastDoor.Year;

    public DateTime DoorDate(int door) => LastDoor.AddDays(door - AdventCalendar.DoorCount);

    public DoorState StateOf(int door, DateTime now, bool isOpened)
    {
        if (isOpened)
        {
            return DoorState.Opened;
        }

        if (now.Date >= DoorDate(door))
        {
            return DoorState.Ready;
        }

        // A taste of what's coming, so the wall isn't all padlocks in November.
        return door == 1 ? DoorState.SneakPeek : DoorState.Locked;
    }

    public int SleepsUntil(int door, DateTime now) => Math.Max(0, (DoorDate(door) - now.Date).Days);

    public int? TodaysDoor(DateTime now)
    {
        var door = AdventCalendar.DoorCount - (LastDoor - now.Date).Days;
        return door is >= 1 and <= AdventCalendar.DoorCount ? door : null;
    }
}

public static class AdventCalendar
{
    public const int DoorCount = 24;

    // Christmas: door 24 on Christmas Eve, and the calendar stays up until Three Kings' Day.
    private const int ChristmasMonth = 12;
    private const int ChristmasLastDoor = 24;
    private const int DaysKeptUp = 13;

    /// <summary>The season a date belongs to, for doors that end on the given day each year.</summary>
    public static AdventSeason SeasonFor(DateTime now, int lastDoorMonth, int lastDoorDay)
    {
        for (var year = now.Year - 1; ; year++)
        {
            var lastDoor = new DateTime(year, lastDoorMonth, Math.Min(lastDoorDay, DateTime.DaysInMonth(year, lastDoorMonth)));
            if (now.Date <= lastDoor.AddDays(DaysKeptUp))
            {
                return new AdventSeason(lastDoor);
            }
        }
    }

    public static AdventSeason Christmas(DateTime now) => SeasonFor(now, ChristmasMonth, ChristmasLastDoor);

    /// <summary>The December a date belongs to. Last season's calendar stays up until Three Kings' Day.</summary>
    public static int SeasonYear(DateTime now) => Christmas(now).Year;

    public static DoorState StateOf(int door, DateTime now, bool isOpened) => Christmas(now).StateOf(door, now, isOpened);

    public static int SleepsUntil(int door, DateTime now) => Christmas(now).SleepsUntil(door, now);

    public static int? TodaysDoor(DateTime now) => Christmas(now).TodaysDoor(now);

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
}
