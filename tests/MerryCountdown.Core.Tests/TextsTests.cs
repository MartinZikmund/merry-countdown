using FluentAssertions;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Tests.Fakes;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class TextsTests
{
    private static readonly FakeStringLocalizer _localizer = new(new Dictionary<string, string>
    {
        ["Day_One"] = "{0} den",
        ["Day_Few"] = "{0} dny",
        ["Day_Many"] = "{0} dní",
    });

    [DataRow(0, "0 dní")]
    [DataRow(1, "1 den")]
    [DataRow(2, "2 dny")]
    [DataRow(4, "4 dny")]
    [DataRow(5, "5 dní")]
    [DataRow(11, "11 dní")]
    [DataRow(22, "22 dní")]
    [DataRow(101, "101 dní")]
    [TestMethod]
    public void Plural_PicksTheCzechForm(int count, string expected) =>
        _localizer.Plural("Day", count).Should().Be(expected);
}
