using FluentAssertions;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class CountdownClockTests
{
    [TestMethod]
    public void CustomDay_InJanuary_CountsAcrossTheNewYear()
    {
        var state = CountdownClock.Compute(new DateTime(2026, 12, 30, 12, 0, 0), month: 1, day: 6, arrivalMinutes: 0);

        state.Phase.Should().Be(CountdownPhase.Counting);
        state.Target.Should().Be(new DateTime(2027, 1, 6));
        state.Sleeps.Should().Be(7);
    }

    [TestMethod]
    public void CustomDay_CelebratesThatDayAndTheNext()
    {
        CountdownClock.Compute(new DateTime(2026, 12, 31, 18, 0, 0), 12, 31, 17 * 60).Phase.Should().Be(CountdownPhase.Celebrating);
        CountdownClock.Compute(new DateTime(2027, 1, 1, 23, 0, 0), 12, 31, 17 * 60).Phase.Should().Be(CountdownPhase.Celebrating);

        var after = CountdownClock.Compute(new DateTime(2027, 1, 2, 0, 0, 0), 12, 31, 17 * 60);
        after.Phase.Should().Be(CountdownPhase.Counting);
        after.Target.Should().Be(new DateTime(2027, 12, 31, 17, 0, 0));
    }

    [TestMethod]
    public void CustomDay_BeforeItsHour_CountsToTheArrival()
    {
        var state = CountdownClock.Compute(new DateTime(2026, 12, 31, 10, 0, 0), 12, 31, 17 * 60);

        state.Sleeps.Should().Be(0);
        state.UntilNextSleep.Should().Be(TimeSpan.FromHours(7));
    }

    [TestMethod]
    public void CustomDay_OnTheTwentyNinthOfFebruary_FallsOnTheTwentyEighthInOtherYears()
    {
        CountdownClock.Compute(new DateTime(2027, 2, 1), 2, 29, 0).Target.Should().Be(new DateTime(2027, 2, 28));
    }

    private static DateTime At(int year, int month, int day, int hour = 12, int minute = 0, int second = 0) =>
        new(year, month, day, hour, minute, second);

    [TestMethod]
    public void Compute_InNovember_CountsSleepsToChristmasDay()
    {
        var state = CountdownClock.Compute(At(2026, 11, 5), BigDay.ChristmasDay, arrivalMinutes: 0);

        state.Phase.Should().Be(CountdownPhase.Counting);
        state.Sleeps.Should().Be(50);
        state.Target.Should().Be(At(2026, 12, 25, 0));
    }

    [TestMethod]
    public void Compute_ChristmasEveFamily_TargetsTheTwentyFourth()
    {
        var state = CountdownClock.Compute(At(2026, 12, 23, 20), BigDay.ChristmasEve, arrivalMinutes: 17 * 60);

        state.Sleeps.Should().Be(1);
        state.Target.Should().Be(At(2026, 12, 24, 17));
        state.Remaining.Should().Be(TimeSpan.FromHours(21));
    }

    [TestMethod]
    public void Compute_OnTheBigDayBeforeArrivalTime_HasNoSleepsLeftButStillCounts()
    {
        var state = CountdownClock.Compute(At(2026, 12, 24, 10), BigDay.ChristmasEve, arrivalMinutes: 17 * 60);

        state.Phase.Should().Be(CountdownPhase.Counting);
        state.Sleeps.Should().Be(0);
        state.Remaining.Should().Be(TimeSpan.FromHours(7));
    }

    [TestMethod]
    public void Compute_AfterArrival_Celebrates()
    {
        var state = CountdownClock.Compute(At(2026, 12, 24, 17, 0, 1), BigDay.ChristmasEve, arrivalMinutes: 17 * 60);

        state.Phase.Should().Be(CountdownPhase.Celebrating);
        state.Remaining.Should().Be(TimeSpan.Zero);
    }

    [DataRow(BigDay.ChristmasEve)]
    [DataRow(BigDay.ChristmasDay)]
    [TestMethod]
    public void Compute_ThroughBoxingDay_KeepsCelebrating(BigDay bigDay)
    {
        CountdownClock.Compute(At(2026, 12, 26, 23, 59), bigDay, 0).Phase.Should().Be(CountdownPhase.Celebrating);
    }

    [TestMethod]
    public void Compute_OnTwentySeventh_RollsOverToNextYear()
    {
        var state = CountdownClock.Compute(At(2026, 12, 27, 0), BigDay.ChristmasDay, 0);

        state.Phase.Should().Be(CountdownPhase.Counting);
        state.Target.Year.Should().Be(2027);
        state.Sleeps.Should().Be(363);
    }

    [TestMethod]
    public void Compute_LeapYear_CountsTheExtraDay()
    {
        CountdownClock.Compute(At(2028, 2, 28), BigDay.ChristmasDay, 0).Sleeps.Should().Be(301);
    }

    [TestMethod]
    public void Compute_WithSleepsLeft_NextSleepChangeIsMidnightEvenWithAnEveningArrival()
    {
        var state = CountdownClock.Compute(At(2026, 12, 9, 18, 30, 12), BigDay.ChristmasEve, arrivalMinutes: 17 * 60);

        state.Sleeps.Should().Be(15);
        state.UntilNextSleep.Should().Be(new TimeSpan(5, 29, 48));
    }

    [TestMethod]
    public void Compute_OnTheBigDayBeforeArrivalTime_NextSleepChangeIsTheArrival()
    {
        var state = CountdownClock.Compute(At(2026, 12, 24, 10), BigDay.ChristmasEve, arrivalMinutes: 17 * 60);

        state.UntilNextSleep.Should().Be(TimeSpan.FromHours(7));
    }

    [TestMethod]
    public void Compute_WhileCelebrating_HasNoNextSleepChange()
    {
        CountdownClock.Compute(At(2026, 12, 25, 8), BigDay.ChristmasDay, 0).UntilNextSleep.Should().Be(TimeSpan.Zero);
    }

    [TestMethod]
    public void NextOccurrence_BirthdayAlreadyPassed_IsNextYear()
    {
        CountdownClock.NextOccurrence(At(2026, 11, 5), month: 3, day: 14).Should().Be(new DateTime(2027, 3, 14));
    }

    [TestMethod]
    public void NextOccurrence_Today_IsToday()
    {
        CountdownClock.NextOccurrence(At(2026, 11, 5, 23), 11, 5).Should().Be(new DateTime(2026, 11, 5));
    }

    [TestMethod]
    public void NextOccurrence_LeapDayInNormalYear_FallsOnFebruaryTwentyEighth()
    {
        CountdownClock.NextOccurrence(At(2026, 11, 5), 2, 29).Should().Be(new DateTime(2027, 2, 28));
    }
}
