namespace MerryCountdown.Core.Season;

/// <summary>Which day the presents arrive. Most of central Europe opens them on the evening of the 24th.</summary>
public enum BigDay
{
    ChristmasEve,
    ChristmasDay,

    /// <summary>A date the family sets itself: New Year's Eve, 6 January, 6 December...</summary>
    Custom,
}

public enum CountdownPhase
{
    Counting,
    Celebrating,
}

/// <param name="UntilNextSleep">Time until <paramref name="Sleeps"/> next changes: midnight, or the arrival once none are left.</param>
public readonly record struct CountdownState(CountdownPhase Phase, DateTime Target, int Sleeps, TimeSpan Remaining, TimeSpan UntilNextSleep);

/// <summary>Works in local wall-clock time on purpose: a child's Christmas is wherever they are.</summary>
public static class CountdownClock
{
    public static CountdownState Compute(DateTime now, BigDay bigDay, int arrivalMinutes) =>
        Compute(now, 12, bigDay == BigDay.ChristmasEve ? 24 : 25, arrivalMinutes);

    /// <summary>Counts to a day that comes round every year, wherever in the year it is.</summary>
    public static CountdownState Compute(DateTime now, int month, int day, int arrivalMinutes)
    {
        var target = TargetIn(now.Year, month, day, arrivalMinutes);

        // A day at the very end of December is still being celebrated on 1 January.
        foreach (var arrival in new[] { target, TargetIn(now.Year - 1, month, day, arrivalMinutes) })
        {
            if (now >= arrival && now < CelebrationEnd(arrival))
            {
                return new CountdownState(CountdownPhase.Celebrating, arrival, 0, TimeSpan.Zero, TimeSpan.Zero);
            }
        }

        if (now > target)
        {
            target = TargetIn(now.Year + 1, month, day, arrivalMinutes);
        }

        var sleeps = (target.Date - now.Date).Days;
        return new CountdownState(CountdownPhase.Counting, target, sleeps, target - now, sleeps > 0 ? now.Date.AddDays(1) - now : target - now);
    }

    /// <summary>The next date a yearly event falls on, today included. Leap-day birthdays land on 28 February.</summary>
    public static DateTime NextOccurrence(DateTime now, int month, int day)
    {
        var occurrence = Occurrence(now.Year, month, day);
        return occurrence >= now.Date ? occurrence : Occurrence(now.Year + 1, month, day);
    }

    private static DateTime Occurrence(int year, int month, int day) =>
        new(year, month, Math.Min(day, DateTime.DaysInMonth(year, month)));

    private static DateTime TargetIn(int year, int month, int day, int arrivalMinutes) =>
        Occurrence(year, month, day).AddMinutes(arrivalMinutes);

    // The big day and the one after. Christmas Eve gets a third, so both Christmases run to the 26th.
    private static DateTime CelebrationEnd(DateTime target) =>
        target.Date.AddDays(target is { Month: 12, Day: 24 } ? 3 : 2);
}
