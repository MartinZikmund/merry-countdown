using System.Text.Json;
using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Services;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class SoundEffectTests
{
    private static readonly DateTime FifthOfDecember = new(2026, 12, 5);

    private readonly FakeSounds _sounds = new();
    private readonly FamilyService _family = new(new FakeFamilyStore(), "en-US");

    private CalendarViewModel Calendar()
    {
        FakeStringLocalizer localizer = new();
        return new CalendarViewModel(_family, new FakeClock(FifthOfDecember), new FakeTicker(), localizer, new FakeGate(), new FakeLauncher(), new KidPickerViewModel(_family, localizer), _sounds);
    }

    private WishListViewModel WishList()
    {
        FakeStringLocalizer localizer = new();
        return new WishListViewModel(_family, new FakeClock(FifthOfDecember), localizer, new FakeGate(), new FakeShare(), new KidPickerViewModel(_family, localizer), _sounds);
    }

    private CountdownViewModel Countdown()
    {
        FakeClock clock = new(FifthOfDecember);
        FakeTicker ticker = new();
        FakeFullVersion full = new();
        return new CountdownViewModel(_family, full, clock, ticker, new FakeStringLocalizer(), new FakeNavigation(), new FakeDisplayMode(), new PhotoBackdrop(_family, full, new FakePhotoLibrary(), ticker), new FakeGate(), _sounds);
    }

    private DisplayViewModel Display()
    {
        FakeTicker ticker = new();
        FakeFullVersion full = new();
        return new DisplayViewModel(_family, full, new FakeClock(FifthOfDecember), ticker, new FakeStringLocalizer(), new FakeDisplayMode(), new PhotoBackdrop(_family, full, new FakePhotoLibrary(), ticker), _sounds);
    }

    private static DoorViewModel Door(CalendarViewModel vm, int number) => vm.Doors.Single(d => d.Number == number);

    [TestMethod]
    public void TapDoor_Locked_KnocksAndRevealsNothing()
    {
        var vm = Calendar();

        vm.TapDoor(Door(vm, 10));

        _sounds.Played.Should().Equal(SoundEffect.DoorLocked);
    }

    [TestMethod]
    public void TapDoor_Ready_StaysQuietBecauseTheSwingAlreadyCreaked()
    {
        var vm = Calendar();

        vm.TapDoor(Door(vm, 5));

        _sounds.Played.Should().BeEmpty();
    }

    [TestMethod]
    public void TapDoor_ReadyWithCalmMotion_CreaksBecauseNothingSwung()
    {
        _family.Settings.CalmMotion = true;
        var vm = Calendar();

        vm.TapDoor(Door(vm, 5));

        _sounds.Played.Should().Equal(SoundEffect.DoorOpen);
    }

    [TestMethod]
    public void TapDoor_AlreadyOpenedWithCalmMotion_Chimes()
    {
        _family.Settings.CalmMotion = true;
        var vm = Calendar();
        vm.TapDoor(Door(vm, 5));
        _sounds.Played.Clear();

        vm.TapDoor(Door(vm, 5));

        _sounds.Played.Should().Equal(SoundEffect.Reveal);
    }

    [TestMethod]
    public void TapDoor_AlreadyOpened_ChimesAgain()
    {
        var vm = Calendar();
        vm.TapDoor(Door(vm, 5));
        _sounds.Played.Clear();

        vm.TapDoor(Door(vm, 5));

        _sounds.Played.Should().Equal(SoundEffect.Reveal);
    }

    [TestMethod]
    public void DoorStartedSwinging_OnTheWall_Creaks()
    {
        Calendar().DoorStartedSwinging();

        _sounds.Played.Should().Equal(SoundEffect.DoorOpen);
    }

    [TestMethod]
    public void ShowPunchline_PlaysTheFanfare()
    {
        var vm = Calendar();
        _ = Enumerable.Range(1, 5).First(number =>
        {
            vm.TapDoor(Door(vm, number));
            return vm.Reveal?.Punchline is not null;
        });
        _sounds.Played.Clear();

        vm.Reveal!.ShowPunchlineCommand.Execute(null);

        _sounds.Played.Should().Equal(SoundEffect.Punchline);
    }

    [TestMethod]
    public void HomeDoor_SwingAndLockedTap_MakeTheirSounds()
    {
        var vm = Countdown();

        vm.DoorStartedSwinging();
        vm.LockedDoorTapped();

        _sounds.Played.Should().Equal(SoundEffect.DoorOpen, SoundEffect.DoorLocked);
    }

    [TestMethod]
    public void SceneTapped_OnHomeAndFamilyDisplay_Twinkles()
    {
        Countdown().SceneTapped();
        Display().SceneTapped();

        _sounds.Played.Should().Equal(SoundEffect.SceneTap, SoundEffect.SceneTap);
    }

    [TestMethod]
    public void FamilyDisplay_LeftAlone_StaysSilent()
    {
        FakeTicker ticker = new();
        FakeFullVersion full = new();
        DisplayViewModel vm = new(_family, full, new FakeClock(FifthOfDecember), ticker, new FakeStringLocalizer(), new FakeDisplayMode(), new PhotoBackdrop(_family, full, new FakePhotoLibrary(), ticker), _sounds);
        vm.Start();

        for (var second = 0; second < 3600; second++)
        {
            ticker.Fire();
        }

        vm.Stop();
        _sounds.Played.Should().BeEmpty();
    }

    [TestMethod]
    public void ChooseSticker_PopsOnPickingButNotOnUnpicking()
    {
        var vm = WishList();
        var sticker = vm.Stickers[0];

        vm.ChooseStickerCommand.Execute(sticker);
        vm.ChooseStickerCommand.Execute(sticker);

        _sounds.Played.Should().Equal(SoundEffect.Sticker);
    }

    [TestMethod]
    public void SendLetter_WithWishes_Whooshes()
    {
        var vm = WishList();
        vm.NewWishText = "Sledge";
        vm.AddWishCommand.Execute(null);

        vm.SendLetterCommand.Execute(null);

        _sounds.Played.Should().Equal(SoundEffect.Letter);
    }

    [TestMethod]
    public void SendLetter_WithNoWishes_StaysSilent()
    {
        WishList().SendLetterCommand.Execute(null);

        _sounds.Played.Should().BeEmpty();
    }

    [TestMethod]
    public void Play_SoundSwitchedOff_MakesNoNoise()
    {
        FakeSounds output = new();
        SoundService service = new(_family, output);
        _family.Settings.SoundEnabled = false;

        service.Play(SoundEffect.Reveal);

        output.Played.Should().BeEmpty();
    }

    [TestMethod]
    public void Play_SoundSwitchedOn_ReachesTheOutput()
    {
        FakeSounds output = new();
        SoundService service = new(_family, output);

        service.Play(SoundEffect.Reveal);

        output.Played.Should().Equal(SoundEffect.Reveal);
    }

    [TestMethod]
    public void Play_CalmMotionOn_StillMakesNoise()
    {
        FakeSounds output = new();
        SoundService service = new(_family, output);
        _family.Settings.CalmMotion = true;

        service.Play(SoundEffect.Reveal);

        output.Played.Should().Equal(SoundEffect.Reveal);
    }

    [TestMethod]
    public void SoundEnabled_MissingFromAnOlderSaveFile_IsOn()
    {
        var data = JsonSerializer.Deserialize("""{ "Settings": {} }""", FamilyJsonContext.Default.FamilyData);

        data!.Settings!.SoundEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void SettingsToggle_SwitchingOff_IsSaved()
    {
        SettingsViewModel vm = new(new FakeStringLocalizer(), new FakeAppPreferences(), new FakeThemeManager(), new FakePreferences(), new FakeApplication(), _family, new FakeGate(), new FakeNavigation(), new FakeDisplayMode(), new FakeLauncher());
        vm.OnNavigatedTo(null);
        vm.SoundEnabled.Should().BeTrue();

        vm.SoundEnabled = false;

        _family.Settings.SoundEnabled.Should().BeFalse();
    }
}
