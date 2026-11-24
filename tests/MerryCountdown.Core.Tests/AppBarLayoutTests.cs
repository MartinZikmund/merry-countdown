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
    [DataRow(400, 300, 138, false, DisplayName = "Phone-sized window: tabs go under the title strip")]
    [DataRow(400, 300, 0, true, DisplayName = "No caption buttons, nothing to make room for")]
    public void SharesRowWithCaptionButtons_ByWidth_KeepsAHandleToDragBy(double bar, double content, double insets, bool expected)
    {
        AppBarLayout.SharesRowWithCaptionButtons(bar, content, insets).Should().Be(expected);
    }
}
