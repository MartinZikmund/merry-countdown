using FluentAssertions;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class DoorWallLayoutTests
{
    [TestMethod]
    public void Fit_LandscapeWindow_PicksTheArrangementWithTheBiggestDoors()
    {
        var layout = DoorWallLayout.Fit(1248, 600);

        layout.Columns.Should().Be(8);
    }

    [TestMethod]
    public void Fit_AnyDoor_KeepsTheProportionsOfADoor()
    {
        var layout = DoorWallLayout.Fit(1248, 600);

        (layout.DoorHeight / layout.DoorWidth).Should().BeApproximately(312d / 216, 0.001);
    }

    [TestMethod]
    public void Fit_RoomForAllDoors_WholeWallFitsWithoutScrolling()
    {
        var layout = DoorWallLayout.Fit(1248, 600);

        var rows = AdventCalendar.DoorCount / layout.Columns;
        var wallHeight = (rows * layout.DoorHeight) + ((rows - 1) * DoorWallLayout.Spacing);
        wallHeight.Should().BeLessThanOrEqualTo(600);
    }

    [TestMethod]
    public void Fit_PhoneTooShortForTheWholeWall_FillsTheWidthAndScrolls()
    {
        var layout = DoorWallLayout.Fit(328, 480);

        layout.Columns.Should().Be(4);
        layout.DoorWidth.Should().Be(76);
    }

    [TestMethod]
    public void Fit_NarrowerThanThreeSmallestDoors_NeverGoesBelowTheSmallestDoor()
    {
        var layout = DoorWallLayout.Fit(150, 300);

        layout.DoorWidth.Should().Be(DoorWallLayout.SmallestDoorWidth);
    }

    [TestMethod]
    public void Fit_HugeWindow_StopsGrowingAtTheLargestDoor()
    {
        var layout = DoorWallLayout.Fit(4000, 3000);

        layout.DoorWidth.Should().Be(DoorWallLayout.LargestDoorWidth);
    }
}
