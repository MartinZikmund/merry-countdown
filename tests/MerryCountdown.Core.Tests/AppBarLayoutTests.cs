using FluentAssertions;
using MerryCountdown.Core.Navigation;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class AppBarLayoutTests
{
    [TestMethod]
    [DataRow(1180, 420, 138, true, DisplayName = "Wide window: tabs sit beside the caption buttons")]
    [DataRow(622, 420, 138, true, DisplayName = "Exactly enough left to drag by")]
    [DataRow(621, 420, 138, false, DisplayName = "One short of a handle")]
    [DataRow(400, 300, 138, false, DisplayName = "Phone-sized window: no room beside the caption buttons")]
    [DataRow(400, 300, 0, true, DisplayName = "No caption buttons, nothing to make room for")]
    public void SharesRowWithCaptionButtons_ByWidth_KeepsAHandleToDragBy(double bar, double content, double insets, bool expected)
    {
        AppBarLayout.SharesRowWithCaptionButtons(bar, content, insets).Should().Be(expected);
    }

    [TestMethod]
    [DataRow(1180, 420, 138, TabsPlacement.Top, DisplayName = "Desktop window: tabs in the title bar")]
    [DataRow(390, 300, 0, TabsPlacement.Bottom, DisplayName = "Phone: tabs under the thumb")]
    [DataRow(639, 300, 0, TabsPlacement.Bottom, DisplayName = "Just under the phone width")]
    [DataRow(640, 300, 0, TabsPlacement.Top, DisplayName = "A phone on its side keeps its height for the page")]
    [DataRow(660, 520, 138, TabsPlacement.Bottom, DisplayName = "Wide enough, but the caption buttons leave no handle")]
    public void PlaceTabs_ByWidth_PicksTheBar(double bar, double content, double insets, TabsPlacement expected)
    {
        AppBarLayout.PlaceTabs(bar, content, insets).Should().Be(expected);
    }
}
