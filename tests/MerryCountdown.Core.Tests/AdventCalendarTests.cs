using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class AdventCalendarTests
{
    [TestMethod]
    public void Season_ForADayOfTheFamilysOwn_EndsItsDoorsOnThatDay()
    {
        var season = AdventCalendar.SeasonFor(new DateTime(2026, 11, 25), 12, 10);

        season.DoorDate(24).Should().Be(new DateTime(2026, 12, 10));
        season.DoorDate(1).Should().Be(new DateTime(2026, 11, 17));
        season.TodaysDoor(new DateTime(2026, 11, 25)).Should().Be(9);
        season.StateOf(9, new DateTime(2026, 11, 25), isOpened: false).Should().Be(DoorState.Ready);
        season.StateOf(10, new DateTime(2026, 11, 25), isOpened: false).Should().Be(DoorState.Locked);
        season.SleepsUntil(12, new DateTime(2026, 11, 25)).Should().Be(3);
    }

    [TestMethod]
    [DataRow(1, 2026, 11, 26, DoorState.SneakPeek, DisplayName = "Door 1 in November")]
    [DataRow(1, 2026, 11, 30, DoorState.SneakPeek, DisplayName = "Door 1 the day before")]
    [DataRow(1, 2026, 12, 1, DoorState.Ready, DisplayName = "Door 1 on its day")]
    [DataRow(2, 2026, 11, 26, DoorState.Locked, DisplayName = "Door 2 in November")]
    [DataRow(24, 2026, 11, 26, DoorState.Locked, DisplayName = "Door 24 in November")]
    public void StateOf_BeforeAdvent_LetsOnlyDoorOneBePeekedAt(int door, int year, int month, int day, DoorState expected)
    {
        var now = new DateTime(year, month, day);

        AdventCalendar.Christmas(now).StateOf(door, now, isOpened: false).Should().Be(expected);
    }

    [TestMethod]
    public void StateOf_OpenedDoorOne_StaysOpenedRatherThanAPeek()
    {
        var now = new DateTime(2026, 11, 26);

        AdventCalendar.Christmas(now).StateOf(1, now, isOpened: true).Should().Be(DoorState.Opened);
    }

    [TestMethod]
    public void Season_ForADayInJanuary_StartsItsDoorsInDecemberAndIsFiledUnderJanuarysYear()
    {
        var season = AdventCalendar.SeasonFor(new DateTime(2026, 12, 20), 1, 6);

        season.DoorDate(24).Should().Be(new DateTime(2027, 1, 6));
        season.DoorDate(1).Should().Be(new DateTime(2026, 12, 14));
        season.Year.Should().Be(2027);
    }

    [TestMethod]
    public void Season_StaysUpThirteenDaysAfterTheLastDoor_ThenMovesToNextYear()
    {
        AdventCalendar.SeasonFor(new DateTime(2026, 12, 23), 12, 10).LastDoor.Should().Be(new DateTime(2026, 12, 10));
        AdventCalendar.SeasonFor(new DateTime(2026, 12, 24), 12, 10).LastDoor.Should().Be(new DateTime(2027, 12, 10));
    }

    [TestMethod]
    public void Season_AfterTheLastDoor_HasNoDoorForToday()
    {
        AdventCalendar.SeasonFor(new DateTime(2026, 12, 12), 12, 10).TodaysDoor(new DateTime(2026, 12, 12)).Should().BeNull();
    }

    [TestMethod]
    [DataRow(BigDay.ChristmasEve)]
    [DataRow(BigDay.ChristmasDay)]
    public void FamilySettings_Christmas_KeepsTheDoorsOnTheFirstToTwentyFourthOfDecember(BigDay bigDay)
    {
        FamilySettings settings = new();
        settings.UseBigDay(bigDay);

        var season = settings.Advent(new DateTime(2026, 11, 25));

        season.DoorDate(1).Should().Be(new DateTime(2026, 12, 1));
        season.DoorDate(24).Should().Be(new DateTime(2026, 12, 24));
    }

    [TestMethod]
    public void FamilySettings_ADayOfItsOwn_MovesTheDoorsWithIt()
    {
        FamilySettings settings = new() { CustomBigDayMonth = 12, CustomBigDayDay = 31 };
        settings.UseBigDay(BigDay.Custom);

        settings.Advent(new DateTime(2026, 12, 8)).TodaysDoor(new DateTime(2026, 12, 8)).Should().Be(1);
    }

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
