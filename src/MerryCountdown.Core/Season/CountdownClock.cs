namespace MerryCountdown.Core.Season;

/// <summary>Which day the presents arrive. Most of central Europe opens them on the evening of the 24th.</summary>
public enum BigDay
{
    ChristmasEve,
    ChristmasDay,
}

public enum CountdownPhase
{
    Counting,
    Celebrating,
}

public readonly record struct CountdownState(CountdownPhase Phase, DateTime Target, int Sleeps, TimeSpan Remaining);

/// <summary>Works in local wall-clock time on purpose: a child's Christmas is wherever they are.</summary>
public static class CountdownClock
{
    private const int LastCelebrationDay = 26;

    public static CountdownState Compute(DateTime now, BigDay bigDay, int arrivalMinutes)
    {
        var target = TargetIn(now.Year, bigDay, arrivalMinutes);

        if (now >= target && now < new DateTime(now.Year, 12, LastCelebrationDay + 1))
        {
            return new CountdownState(CountdownPhase.Celebrating, target, 0, TimeSpan.Zero);
        }

        if (now > target)
        {
            target = TargetIn(now.Year + 1, bigDay, arrivalMinutes);
        }

        return new CountdownState(CountdownPhase.Counting, target, (target.Date - now.Date).Days, target - now);
    }

    /// <summary>The next date a yearly event falls on, today included. Leap-day birthdays land on 28 February.</summary>
    public static DateTime NextOccurrence(DateTime now, int month, int day)
    {
        var occurrence = Occurrence(now.Year, month, day);
        return occurrence >= now.Date ? occurrence : Occurrence(now.Year + 1, month, day);
    }

    private static DateTime Occurrence(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));

    private static DateTime TargetIn(int year, BigDay bigDay, int arrivalMinutes) =>
        new DateTime(year, 12, bigDay == BigDay.ChristmasEve ? 24 : 25).AddMinutes(arrivalMinutes);
}
