using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class WindowShellViewModelTests
{
    private readonly FamilyService _family = new(new FakeFamilyStore(), "en-US");

    private WindowShellViewModel Create(DateTime now) =>
        new(new FakeNavigation(), _family, new FakeClock(now), new FakeTicker(), new FakeStringLocalizer());

    [TestMethod]
    public void HasWaitingDoor_TodaysDoorUnopened_ShowsItsNumber()
    {
        var vm = Create(new DateTime(2026, 12, 10));

        vm.HasWaitingDoor.Should().BeTrue();
        vm.WaitingDoorNumber.Should().Be(10);
    }

    [TestMethod]
    public void HasWaitingDoor_DoorGetsOpened_ClearsStraightAway()
    {
        var vm = Create(new DateTime(2026, 12, 10));

        _family.OpenDoor(_family.ActiveKid, 2026, 10);

        vm.HasWaitingDoor.Should().BeFalse();
    }

    [TestMethod]
    [DataRow(2026, 11, 6)]
    [DataRow(2026, 12, 25)]
    [DataRow(2027, 1, 3)]
    public void HasWaitingDoor_OutsideTheDoorDays_StaysQuiet(int year, int month, int day)
    {
        var vm = Create(new DateTime(year, month, day));

        vm.HasWaitingDoor.Should().BeFalse();
    }

    [TestMethod]
    public void HasWaitingDoor_NewDayStarts_ComesBack()
    {
        FakeClock clock = new(new DateTime(2026, 12, 10, 23, 59, 59));
        FakeTicker ticker = new();
        WindowShellViewModel vm = new(new FakeNavigation(), _family, clock, ticker, new FakeStringLocalizer());
        _family.OpenDoor(_family.ActiveKid, 2026, 10);

        clock.Now = new DateTime(2026, 12, 11);
        ticker.Fire();

        vm.HasWaitingDoor.Should().BeTrue();
        vm.WaitingDoorNumber.Should().Be(11);
    }

    [TestMethod]
    public void DoorsTabName_WithAWaitingDoor_TellsScreenReadersToo()
    {
        var vm = Create(new DateTime(2026, 12, 10));

        vm.DoorsTabName.Should().Be("NavDoorWaiting");

        _family.OpenDoor(_family.ActiveKid, 2026, 10);

        vm.DoorsTabName.Should().Be("NavCalendar");
    }
}
