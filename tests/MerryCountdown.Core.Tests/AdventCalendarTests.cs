using FluentAssertions;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class AdventCalendarTests
{
    [DataRow(2026, 11, 5, 2026)]
    [DataRow(2026, 12, 31, 2026)]
    [DataRow(2027, 1, 6, 2026)]
    [DataRow(2027, 1, 7, 2027)]
    [TestMethod]
    public void SeasonYear_KeepsLastSeasonUntilThreeKingsDay(int year, int month, int day, int expected)
    {
        AdventCalendar.SeasonYear(new DateTime(year, month, day)).Should().Be(expected);
    }

    [TestMethod]
    public void StateOf_BeforeItsDay_IsLocked()
    {
        AdventCalendar.StateOf(5, new DateTime(2026, 12, 4, 23, 59, 59), isOpened: false).Should().Be(DoorState.Locked);
    }

    [TestMethod]
    public void StateOf_AtMidnightOfItsDay_IsReady()
    {
        AdventCalendar.StateOf(5, new DateTime(2026, 12, 5), isOpened: false).Should().Be(DoorState.Ready);
    }

    [TestMethod]
    public void StateOf_MissedDay_StaysReady()
    {
        AdventCalendar.StateOf(1, new DateTime(2026, 12, 20), isOpened: false).Should().Be(DoorState.Ready);
    }

    [TestMethod]
    public void StateOf_Opened_IsOpened()
    {
        AdventCalendar.StateOf(1, new DateTime(2026, 12, 20), isOpened: true).Should().Be(DoorState.Opened);
    }

    [TestMethod]
    public void StateOf_InJanuaryBeforeReset_MissedDoorsStillOpen()
    {
        AdventCalendar.StateOf(24, new DateTime(2027, 1, 3), isOpened: false).Should().Be(DoorState.Ready);
    }

    [TestMethod]
    public void SleepsUntil_CountsMidnightsToTheDoor()
    {
        AdventCalendar.SleepsUntil(3, new DateTime(2026, 11, 30, 18, 0, 0)).Should().Be(3);
        AdventCalendar.SleepsUntil(3, new DateTime(2026, 12, 5)).Should().Be(0);
    }

    [TestMethod]
    public void TodaysDoor_OnlyDuringFirstTwentyFourDaysOfDecember()
    {
        AdventCalendar.TodaysDoor(new DateTime(2026, 12, 24)).Should().Be(24);
        AdventCalendar.TodaysDoor(new DateTime(2026, 12, 25)).Should().BeNull();
        AdventCalendar.TodaysDoor(new DateTime(2026, 11, 30)).Should().BeNull();
    }

    [TestMethod]
    public void LayoutFor_IsAStablePermutationThatChangesYearly()
    {
        var a = AdventCalendar.LayoutFor(2026);

        a.Should().BeEquivalentTo(Enumerable.Range(1, 24));
        a.Should().Equal(AdventCalendar.LayoutFor(2026));
        a.Should().NotEqual(Enumerable.Range(1, 24));
        a.Should().NotEqual(AdventCalendar.LayoutFor(2027));
    }
}
