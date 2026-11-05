using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class DefaultsTests
{
    [DataRow("cs-CZ", BigDay.ChristmasEve, GiftBringer.Jezisek)]
    [DataRow("sk-SK", BigDay.ChristmasEve, GiftBringer.Jezisek)]
    [DataRow("de-DE", BigDay.ChristmasEve, GiftBringer.Santa)]
    [DataRow("pl-PL", BigDay.ChristmasEve, GiftBringer.Santa)]
    [DataRow("en-US", BigDay.ChristmasDay, GiftBringer.Santa)]
    [DataRow("en-GB", BigDay.ChristmasDay, GiftBringer.Santa)]
    [TestMethod]
    public void ForCulture_MatchesLocalTradition(string culture, BigDay bigDay, GiftBringer bringer)
    {
        var settings = FamilySettings.ForCulture(culture);

        settings.BigDay.Should().Be(bigDay);
        settings.GiftBringer.Should().Be(bringer);
    }
}
