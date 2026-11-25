using FluentAssertions;
using MerryCountdown.Core.Services;
using MerryCountdown.Core.Tests.Fakes;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class WelcomeTipsTests
{
    [TestMethod]
    public void Begin_OnAFreshInstall_PointsAtCustomizing()
    {
        WelcomeTips tips = new(new FakeAppPreferences());

        tips.Begin();

        tips.Current.Should().Be(WelcomeTip.Customize);
    }

    [TestMethod]
    public void Begin_AfterTheTipsWereSeen_ShowsNothing()
    {
        WelcomeTips tips = new(new FakeAppPreferences { HasSeenWelcomeTips = true });

        tips.Begin();

        tips.Current.Should().Be(WelcomeTip.None);
    }

    [TestMethod]
    public void Begin_WhileATipIsShowing_KeepsThatTip()
    {
        WelcomeTips tips = new(new FakeAppPreferences());
        tips.Begin();
        tips.Next();

        tips.Begin();

        tips.Current.Should().Be(WelcomeTip.Settings);
    }

    [TestMethod]
    public void Next_FromCustomizing_PointsAtSettings()
    {
        FakeAppPreferences preferences = new();
        WelcomeTips tips = new(preferences);
        tips.Begin();

        tips.Next();

        tips.Current.Should().Be(WelcomeTip.Settings);
        preferences.HasSeenWelcomeTips.Should().BeFalse();
    }

    [TestMethod]
    public void Next_FromSettings_EndsAndRemembers()
    {
        FakeAppPreferences preferences = new();
        WelcomeTips tips = new(preferences);
        tips.Begin();
        tips.Next();

        tips.Next();

        tips.Current.Should().Be(WelcomeTip.None);
        preferences.HasSeenWelcomeTips.Should().BeTrue();
    }

    [TestMethod]
    public void Finish_OnTheFirstTip_EndsAndRemembers()
    {
        FakeAppPreferences preferences = new();
        WelcomeTips tips = new(preferences);
        tips.Begin();

        tips.Finish();

        tips.Current.Should().Be(WelcomeTip.None);
        preferences.HasSeenWelcomeTips.Should().BeTrue();
    }

    [TestMethod]
    public void Finish_BeforeAnyTipShowed_RemembersNothing()
    {
        FakeAppPreferences preferences = new();
        WelcomeTips tips = new(preferences);

        tips.Finish();

        preferences.HasSeenWelcomeTips.Should().BeFalse();
    }

    [TestMethod]
    public void Changed_IsRaisedForEachStep()
    {
        WelcomeTips tips = new(new FakeAppPreferences());
        List<WelcomeTip> seen = [];
        tips.Changed += (_, _) => seen.Add(tips.Current);

        tips.Begin();
        tips.Next();
        tips.Next();

        seen.Should().Equal(WelcomeTip.Customize, WelcomeTip.Settings, WelcomeTip.None);
    }
}
