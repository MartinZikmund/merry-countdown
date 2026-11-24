using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Services;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public sealed class CompactOverlayTests
{
    private static readonly DateTime Now = new(2026, 12, 9, 18, 30, 0);

    private readonly FamilyService _family = new(new FakeFamilyStore(), "en-US");
    private readonly FakeTicker _ticker = new();
    private readonly FakeClock _clock = new(Now);
    private readonly FakeFullVersion _full = new();
    private readonly FakeSounds _sounds = new();

    private CountdownViewModel Home(ICompactOverlayService overlay) =>
        new(_family, _full, _clock, _ticker, new FakeStringLocalizer(), new FakeNavigation(), new FakeDisplayMode(), new PhotoBackdrop(_family, _full, new FakePhotoLibrary(), _ticker), new FakeGate(), _sounds, overlay, new FakeMessages());

    private CompactOverlayViewModel Overlay(ICompactOverlayService overlay) =>
        new(_family, _full, _clock, _ticker, new FakeStringLocalizer(), overlay, new PhotoBackdrop(_family, _full, new FakePhotoLibrary(), _ticker), _sounds);

    [TestMethod]
    [DataRow(true)]
    [DataRow(false)]
    public void Home_CanUseCompactOverlay_FollowsWhatTheHeadSupports(bool isSupported)
    {
        Home(new FakeCompactOverlay(isSupported)).CanUseCompactOverlay.Should().Be(isSupported);
    }

    [TestMethod]
    public void Home_EnterCompactOverlayCommand_EntersTheOverlay()
    {
        FakeCompactOverlay overlay = new();

        Home(overlay).EnterCompactOverlayCommand.Execute(null);

        overlay.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void Home_WhileOverlayIsActive_PausesItsHiddenScene()
    {
        FakeCompactOverlay overlay = new();
        var vm = Home(overlay);
        vm.ViewLoaded();

        overlay.Enter();
        vm.IsScenePaused.Should().BeTrue();

        overlay.Exit();
        vm.IsScenePaused.Should().BeFalse();
    }

    [TestMethod]
    public void Home_AfterUnload_StopsFollowingTheOverlay()
    {
        FakeCompactOverlay overlay = new();
        var vm = Home(overlay);
        vm.ViewLoaded();
        vm.ViewUnloaded();

        overlay.Enter();

        vm.IsScenePaused.Should().BeFalse();
    }

    [TestMethod]
    public void Overlay_Created_ShowsTheFamilysSignAndScene()
    {
        _family.Settings.Sign = SignDesign.Station;
        _family.Settings.CountdownStyle = CountdownStyle.SleepsAndClock;
        _family.Settings.SceneId = "globe";
        _family.Settings.Snow = SnowLevel.Blizzard;
        _family.Settings.CalmMotion = true;

        var vm = Overlay(new FakeCompactOverlay());

        vm.Sign.Design.Should().Be(SignDesign.Station);
        vm.Sign.Style.Should().Be(CountdownStyle.SleepsAndClock);
        vm.SceneId.Should().Be("globe");
        vm.Snow.Should().Be(SnowLevel.Blizzard);
        vm.CalmMotion.Should().BeTrue();
    }

    [TestMethod]
    public void Overlay_EverySecondWhileStarted_MovesTheCountdown()
    {
        var vm = Overlay(new FakeCompactOverlay());
        vm.Start();
        var before = vm.Sign.Seconds;

        _clock.Now = Now.AddSeconds(1);
        _ticker.Fire();

        vm.Sign.Seconds.Should().NotBe(before);
    }

    [TestMethod]
    public void Overlay_Stopped_LeavesTheCountdownAlone()
    {
        var vm = Overlay(new FakeCompactOverlay());
        vm.Start();
        vm.Stop();
        var before = vm.Sign.Seconds;

        _clock.Now = Now.AddSeconds(1);
        _ticker.Fire();

        vm.Sign.Seconds.Should().Be(before);
    }

    [TestMethod]
    public void Overlay_ExitCommand_LeavesTheOverlay()
    {
        FakeCompactOverlay overlay = new();
        overlay.Enter();

        Overlay(overlay).ExitCommand.Execute(null);

        overlay.IsActive.Should().BeFalse();
    }
}
