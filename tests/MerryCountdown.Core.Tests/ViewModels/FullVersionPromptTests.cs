using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class FullVersionPromptTests
{
    private static (CountdownViewModel Vm, FakeNavigation Nav, FakeGate Gate, FakeMessages Messages) Create()
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeTicker ticker = new();
        FakeFullVersion full = new(false);
        FakeNavigation nav = new();
        FakeGate gate = new();
        FakeMessages messages = new();
        return (new CountdownViewModel(family, full, new FakeClock(new DateTime(2026, 11, 5)), ticker, new FakeStringLocalizer(), nav, new FakeDisplayMode(), new PhotoBackdrop(family, full, new FakePhotoLibrary(), ticker), gate, new FakeSounds(), new FakeCompactOverlay(), messages), nav, gate, messages);
    }

    private static SceneOptionViewModel LockedScene(CountdownViewModel vm) =>
        vm.Scenes.First(s => s.IsLocked && s.Id != CountdownViewModel.PhotosOptionId);

    private static SceneOptionViewModel Photos(CountdownViewModel vm) =>
        vm.Scenes.Single(s => s.Id == CountdownViewModel.PhotosOptionId);

    [TestMethod]
    public async Task SelectScene_Locked_ExplainsThenAsksAGrownUpThenOpensTheirCorner()
    {
        var (vm, nav, gate, messages) = Create();
        var before = vm.SceneId;

        await vm.SelectSceneCommand.ExecuteAsync(LockedScene(vm));

        messages.Confirmations.Should().ContainSingle().Which.Should().Be(("FullVersionLockedTitle", "FullVersionPrompt"));
        gate.Asked.Should().Be(1);
        nav.Visited.Should().Equal(typeof(ParentZoneViewModel));
        vm.SceneId.Should().Be(before);
    }

    [TestMethod]
    public async Task SelectScene_Locked_Declined_StaysPutWithoutTheGate()
    {
        var (vm, nav, gate, messages) = Create();
        messages.ConfirmResult = false;

        await vm.SelectSceneCommand.ExecuteAsync(LockedScene(vm));

        gate.Asked.Should().Be(0);
        nav.Visited.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectScene_Locked_WrongAnswer_StaysPut()
    {
        var (vm, nav, gate, _) = Create();
        gate.Passes = false;

        await vm.SelectSceneCommand.ExecuteAsync(LockedScene(vm));

        nav.Visited.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SelectScene_PhotosInFreeVersion_ExplainsFirstToo()
    {
        var (vm, nav, gate, messages) = Create();

        await vm.SelectSceneCommand.ExecuteAsync(Photos(vm));

        messages.Confirmations.Should().ContainSingle();
        gate.Asked.Should().Be(1);
        nav.Visited.Should().Equal(typeof(ParentZoneViewModel));
    }
}
