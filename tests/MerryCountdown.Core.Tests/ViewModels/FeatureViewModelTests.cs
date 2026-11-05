using FluentAssertions;
using MerryCountdown.Core.Content;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class CountdownViewModelTests
{
    private static (CountdownViewModel Vm, FamilyService Family, FakeClock Clock, FakeTicker Ticker, FakeNavigation Nav, FakeDisplayMode Display, FakeFullVersion Full) Create(DateTime now, bool isFull = false)
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeClock clock = new(now);
        FakeTicker ticker = new();
        FakeNavigation nav = new();
        FakeDisplayMode display = new();
        FakeFullVersion full = new(isFull);
        return (new CountdownViewModel(family, full, clock, ticker, new FakeStringLocalizer(), nav, display, new PhotoBackdrop(family, full, new FakePhotoLibrary(), ticker), new FakeGate()), family, clock, ticker, nav, display, full);
    }

    [TestMethod]
    public void Refresh_ShowsSleepsAndClockParts()
    {
        var (vm, _, _, _, _, _, _) = Create(new DateTime(2026, 12, 23, 22, 30, 15));

        vm.Sign.SleepsNumber.Should().Be("2");
        vm.Sign.Days.Should().Be("01");
        vm.Sign.Hours.Should().Be("01");
        vm.Sign.Minutes.Should().Be("29");
        vm.Sign.Seconds.Should().Be("45");
    }

    [TestMethod]
    public void Tick_MovesTheClockOnlyWhileTheViewIsLoaded()
    {
        var (vm, _, clock, ticker, _, _, _) = Create(new DateTime(2026, 12, 20));
        vm.ViewLoaded();

        clock.Now = clock.Now.AddSeconds(1);
        ticker.Fire();
        vm.Sign.Seconds.Should().Be("59");

        vm.ViewUnloaded();
        clock.Now = clock.Now.AddSeconds(1);
        ticker.Fire();
        vm.Sign.Seconds.Should().Be("59");
    }

    [TestMethod]
    public void AfterArrival_Celebrates()
    {
        var (vm, _, _, _, _, _, _) = Create(new DateTime(2026, 12, 25, 8, 0, 0));

        vm.Sign.IsCelebrating.Should().BeTrue();
        vm.Sign.IsCounting.Should().BeFalse();
    }

    [TestMethod]
    public void SelectScene_LockedInFreeVersion_GoesToSettingsAndKeepsTheScene()
    {
        var (vm, _, _, _, nav, _, _) = Create(new DateTime(2026, 11, 5));
        var before = vm.SceneId;

        vm.SelectSceneCommand.Execute(vm.Scenes.First(s => s.IsLocked && s.Id != CountdownViewModel.PhotosOptionId));

        vm.SceneId.Should().Be(before);
        nav.Visited.Should().Contain(typeof(SettingsViewModel));
    }

    [TestMethod]
    public void Events_ShowOnlyInFullVersion_SoonestFirst()
    {
        var (vm, family, _, _, _, _, _) = Create(new DateTime(2026, 11, 5), isFull: true);
        vm.ViewLoaded();

        family.AddEvent("Later", 3, 1, "cake");
        family.AddEvent("Sooner", 12, 1, "cake");

        vm.Events.Select(e => e.Name).Should().Equal("Sooner", "Later");
    }

    [TestMethod]
    public void Events_HiddenInFreeVersion()
    {
        var (vm, family, _, _, _, _, _) = Create(new DateTime(2026, 11, 5));
        vm.ViewLoaded();

        family.AddEvent("Birthday", 12, 1, "cake");

        vm.HasEvents.Should().BeFalse();
    }

    [TestMethod]
    public void EnterFamilyDisplay_TurnsTheModeOn()
    {
        var (vm, _, _, _, _, display, _) = Create(new DateTime(2026, 11, 5));

        vm.EnterFamilyDisplayCommand.Execute(null);

        display.IsActive.Should().BeTrue();
    }
}

[TestClass]
public class CalendarViewModelTests
{
    private static (CalendarViewModel Vm, FamilyService Family, FakeClock Clock, FakeGate Gate, FakeLauncher Launcher) Create(DateTime now)
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeClock clock = new(now);
        FakeGate gate = new();
        FakeLauncher launcher = new();
        FakeStringLocalizer localizer = new();
        return (new CalendarViewModel(family, clock, new FakeTicker(), localizer, gate, launcher, new KidPickerViewModel(family, localizer)), family, clock, gate, launcher);
    }

    private static DoorViewModel Door(CalendarViewModel vm, int number) => vm.Doors.Single(d => d.Number == number);

    [TestMethod]
    public void OnNavigatedTo_WithTodaysDoor_OpensItAndRevealsIt()
    {
        var (vm, family, _, _, _) = Create(new DateTime(2026, 12, 5));

        vm.OnNavigatedTo(5);

        vm.Reveal.Should().NotBeNull();
        family.IsDoorOpened(family.ActiveKid, 2026, 5).Should().BeTrue();
    }

    [TestMethod]
    public void OnNavigatedTo_WithoutADoor_JustShowsTheWall()
    {
        var (vm, _, _, _, _) = Create(new DateTime(2026, 12, 5));

        vm.OnNavigatedTo(null);

        vm.Reveal.Should().BeNull();
    }

    [TestMethod]
    public void Doors_AreAllTwentyFour()
    {
        Create(new DateTime(2026, 12, 5)).Vm.Doors.Should().HaveCount(24);
    }

    [TestMethod]
    public void TapDoor_Locked_StaysShutJigglesAndSaysHowLong()
    {
        var (vm, family, _, _, _) = Create(new DateTime(2026, 12, 5));
        var door = Door(vm, 10);
        var jiggled = false;
        door.Jiggled += (_, _) => jiggled = true;

        vm.TapDoor(door);

        jiggled.Should().BeTrue();
        vm.Reveal.Should().BeNull();
        vm.Hint.Should().NotBeEmpty();
        family.IsDoorOpened(family.ActiveKid, 2026, 10).Should().BeFalse();
    }

    [TestMethod]
    public void TapDoor_Ready_OpensItAndRevealsItsSurprise()
    {
        var (vm, family, _, _, _) = Create(new DateTime(2026, 12, 5));

        vm.TapDoor(Door(vm, 5));

        family.IsDoorOpened(family.ActiveKid, 2026, 5).Should().BeTrue();
        Door(vm, 5).State.Should().Be(DoorState.Opened);
        vm.Reveal!.Number.Should().Be(5);
    }

    [TestMethod]
    public void Reveal_CarriesTheGrownUpsMessageForThisKid()
    {
        var (vm, family, _, _, _) = Create(new DateTime(2026, 12, 5));
        family.SetDoorMessage(family.ActiveKid, 5, "Look under your pillow!");

        vm.TapDoor(Door(vm, 5));

        vm.Reveal!.PersonalMessage.Should().Be("Look under your pillow!");
    }

    [TestMethod]
    public void SwitchingKid_ShowsThatKidsOwnDoors()
    {
        var (vm, family, _, _, _) = Create(new DateTime(2026, 12, 5));
        vm.ViewLoaded();
        vm.TapDoor(Door(vm, 5));
        var ema = family.AddKid("Ema");

        family.SetActiveKid(ema);

        Door(vm, 5).State.Should().Be(DoorState.Ready);
        vm.Reveal.Should().BeNull();
    }

    [TestMethod]
    public async Task OpenVideo_AsksAGrownUpFirst()
    {
        var (vm, _, _, gate, launcher) = Create(new DateTime(2026, 12, 20));
        var videoDoor = Enumerable.Range(1, 20).First(d => DoorLibrary.Load("en").For(d, 2026).Kind == DoorKind.Video);
        vm.TapDoor(Door(vm, videoDoor));

        gate.Passes = false;
        await vm.OpenVideoCommand.ExecuteAsync(null);
        launcher.Launched.Should().BeEmpty();

        gate.Passes = true;
        await vm.OpenVideoCommand.ExecuteAsync(null);
        launcher.Launched.Should().ContainSingle();
    }

    [TestMethod]
    public void Punchline_HiddenUntilAsked()
    {
        var (vm, _, _, _, _) = Create(new DateTime(2026, 12, 5));
        vm.TapDoor(Door(vm, 1));

        vm.Reveal!.CanRevealPunchline.Should().BeTrue();

        vm.Reveal.ShowPunchlineCommand.Execute(null);

        vm.Reveal.IsPunchlineShown.Should().BeTrue();
        vm.Reveal.CanRevealPunchline.Should().BeFalse();
    }
}

[TestClass]
public class WishListViewModelTests
{
    private static (WishListViewModel Vm, FamilyService Family, FakeGate Gate, FakeShare Share) Create()
    {
        FamilyService family = new(new FakeFamilyStore(), "cs-CZ");
        FakeGate gate = new();
        FakeShare share = new();
        FakeStringLocalizer localizer = new();
        return (new WishListViewModel(family, new FakeClock(new DateTime(2026, 11, 5)), localizer, gate, share, new KidPickerViewModel(family, localizer)), family, gate, share);
    }

    [TestMethod]
    public void Salutation_FollowsTheChosenGiftBringer()
    {
        Create().Vm.Salutation.Should().Be("LetterDear_Jezisek");
    }

    [TestMethod]
    public void AddWish_GoesOnTheListAndClearsTheBox()
    {
        var (vm, _, _, _) = Create();
        vm.NewWishText = "Sáně";

        vm.AddWishCommand.Execute(null);

        vm.Wishes.Should().ContainSingle(w => w.Label == "Sáně");
        vm.NewWishText.Should().BeEmpty();
    }

    [TestMethod]
    public void AddWish_NeedsTextOrAPicture()
    {
        var (vm, _, _, _) = Create();

        vm.AddWishCommand.CanExecute(null).Should().BeFalse();

        vm.ChooseStickerCommand.Execute(vm.Stickers[0]);

        vm.AddWishCommand.CanExecute(null).Should().BeTrue();
    }

    [TestMethod]
    public void SwitchingKid_ShowsTheirOwnList()
    {
        var (vm, family, _, _) = Create();
        vm.ViewLoaded();
        vm.NewWishText = "Sáně";
        vm.AddWishCommand.Execute(null);
        var ema = family.AddKid("Ema");

        family.SetActiveKid(ema);

        vm.Wishes.Should().BeEmpty();
        vm.KidName.Should().Be("Ema");

        family.SetActiveKid(family.Kids[0]);

        vm.Wishes.Should().ContainSingle();
    }

    [TestMethod]
    public void SendLetter_MarksItSentAndWriteMoreReopensIt()
    {
        var (vm, _, _, _) = Create();
        vm.NewWishText = "Sáně";
        vm.AddWishCommand.Execute(null);

        vm.SendLetterCommand.Execute(null);
        vm.IsSent.Should().BeTrue();

        vm.WriteMoreCommand.Execute(null);
        vm.IsSent.Should().BeFalse();
        vm.Wishes.Should().ContainSingle();
    }

    [TestMethod]
    public void LetterSentLastYear_IsNotStillSent()
    {
        var (vm, family, _, _) = Create();
        vm.NewWishText = "Sáně";
        vm.AddWishCommand.Execute(null);
        family.SendLetter(2025);

        vm.ViewLoaded();

        vm.IsSent.Should().BeFalse();
    }

    [TestMethod]
    public async Task Share_NeedsAGrownUp()
    {
        var (vm, _, gate, share) = Create();
        vm.NewWishText = "Sáně";
        vm.AddWishCommand.Execute(null);

        gate.Passes = false;
        await vm.ShareWithGrownUpsCommand.ExecuteAsync(null);
        share.LastText.Should().BeNull();

        gate.Passes = true;
        await vm.ShareWithGrownUpsCommand.ExecuteAsync(null);
        share.LastText.Should().Contain("Sáně");
    }

    [TestMethod]
    public void KidName_EditingSavesIt()
    {
        var (vm, family, _, _) = Create();

        vm.KidName = "Ema";

        family.ActiveKid.Name.Should().Be("Ema");
    }
}

[TestClass]
public class DisplayViewModelTests
{
    private static (DisplayViewModel Vm, FamilyService Family, FakeTicker Ticker, FakeDisplayMode Mode) Create(DateTime now, bool isFull = true)
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeTicker ticker = new();
        FakeDisplayMode mode = new();
        FakeFullVersion full = new(isFull);
        return (new DisplayViewModel(family, full, new FakeClock(now), ticker, new FakeStringLocalizer(), mode, new PhotoBackdrop(family, full, new FakePhotoLibrary(), ticker)), family, ticker, mode);
    }

    [TestMethod]
    public void Scene_RotatesAfterHalfAnHourWhenEnabled()
    {
        var (vm, _, ticker, _) = Create(new DateTime(2026, 11, 5));
        vm.Start();
        var first = vm.SceneId;

        for (var i = 0; i < 1800; i++)
        {
            ticker.Fire();
        }

        vm.SceneId.Should().NotBe(first);
    }

    [TestMethod]
    public void Scene_StaysPutWhenRotationIsOff()
    {
        var (vm, family, ticker, _) = Create(new DateTime(2026, 11, 5));
        family.Settings.DisplayRotatesScenes = false;
        vm.Start();
        var first = vm.SceneId;

        for (var i = 0; i < 1800; i++)
        {
            ticker.Fire();
        }

        vm.SceneId.Should().Be(first);
    }

    [TestMethod]
    public void FreeVersion_RotatesOnlyThroughFreeScenes()
    {
        var (vm, _, _, _) = Create(new DateTime(2026, 11, 5), isFull: false);

        for (var i = 0; i < 6; i++)
        {
            vm.NextScene();
            SceneCatalog.All.Single(s => s.Id == vm.SceneId).RequiresFullVersion.Should().BeFalse();
        }
    }

    [TestMethod]
    public void Content_DriftsSoNothingBurnsIn()
    {
        var (vm, _, ticker, _) = Create(new DateTime(2026, 11, 5));
        vm.Start();
        var start = (vm.DriftX, vm.DriftY);

        for (var i = 0; i < 100; i++)
        {
            ticker.Fire();
        }

        (vm.DriftX, vm.DriftY).Should().NotBe(start);
        Math.Abs(vm.DriftX).Should().BeLessThanOrEqualTo(24);
    }

    [TestMethod]
    public void Hint_FadesAndComesBackOnDemand()
    {
        var (vm, _, ticker, _) = Create(new DateTime(2026, 11, 5));
        vm.Start();
        vm.IsHintVisible.Should().BeTrue();

        for (var i = 0; i < 8; i++)
        {
            ticker.Fire();
        }

        vm.IsHintVisible.Should().BeFalse();

        vm.ShowHint();
        vm.IsHintVisible.Should().BeTrue();
    }

    [TestMethod]
    public void Kids_EachGetTheirOwnRow()
    {
        var (vm, family, ticker, _) = Create(new DateTime(2026, 12, 5));
        vm.Start();
        var ema = family.AddKid("Ema");
        family.OpenDoor(ema, 2026, 5);

        ticker.Fire();

        vm.Kids.Should().HaveCount(2);
        vm.Kids[1].Name.Should().Be("Ema");
        vm.Kids[1].Status.Should().Contain("DisplayDoorOpened");
        vm.Kids[0].Status.Should().Contain("DisplayDoorWaiting");
    }

    [TestMethod]
    public void Exit_LeavesTheMode()
    {
        var (vm, _, _, mode) = Create(new DateTime(2026, 11, 5));
        mode.Enter();

        vm.ExitCommand.Execute(null);

        mode.IsActive.Should().BeFalse();
    }
}

[TestClass]
public class ParentZoneViewModelTests
{
    private static (ParentZoneViewModel Vm, FamilyService Family, FakeFullVersion Full, FakeMessages Messages) Create(bool isFull = true)
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeFullVersion full = new(isFull);
        FakeMessages messages = new();
        return (new ParentZoneViewModel(family, full, new FakeStringLocalizer(), messages, new FakeClock(new DateTime(2026, 11, 5)), new FakePhotoLibrary()), family, full, messages);
    }

    [TestMethod]
    public void AddKid_AppearsInTheList()
    {
        var (vm, family, _, _) = Create();
        vm.NewKidName = "Ema";

        vm.AddKidCommand.Execute(null);

        family.Kids.Should().HaveCount(2);
        vm.Kids.Should().HaveCount(2);
        vm.NewKidName.Should().BeEmpty();
    }

    [TestMethod]
    public void AddKid_IsAvailableInTheFreeVersion()
    {
        var (vm, family, _, _) = Create(isFull: false);
        vm.NewKidName = "Ema";

        vm.AddKidCommand.Execute(null);

        family.Kids.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task RemoveKid_AsksFirst()
    {
        var (vm, family, _, messages) = Create();
        family.AddKid("Ema");
        vm.ViewLoaded();

        messages.ConfirmResult = false;
        await vm.RemoveKidCommand.ExecuteAsync(vm.Kids[1]);
        family.Kids.Should().HaveCount(2);

        messages.ConfirmResult = true;
        await vm.RemoveKidCommand.ExecuteAsync(vm.Kids[1]);
        family.Kids.Should().HaveCount(1);
    }

    [TestMethod]
    public void LastKid_CannotBeRemoved()
    {
        Create().Vm.Kids[0].CanRemove.Should().BeFalse();
    }

    [TestMethod]
    public void DoorMessage_SavesPerKidAndDoor()
    {
        var (vm, family, _, _) = Create();
        vm.MessageDoor = 7;
        vm.MessageText = "Hello";

        vm.SaveMessageCommand.Execute(null);

        family.ActiveKid.DoorMessages[7].Should().Be("Hello");
    }

    [TestMethod]
    public void ChoosingAnotherDoor_LoadsItsMessage()
    {
        var (vm, family, _, _) = Create();
        family.SetDoorMessage(family.ActiveKid, 3, "Three");

        vm.MessageDoor = 3;

        vm.MessageText.Should().Be("Three");
    }

    [TestMethod]
    public void ArrivalTime_ChangesTheCountdownTargetInFullVersion()
    {
        var (vm, family, _, _) = Create();

        vm.ArrivalTime = TimeSpan.FromHours(6);

        family.Settings.ArrivalMinutes.Should().Be(360);
    }

    [TestMethod]
    public void ArrivalTime_IgnoredInFreeVersion()
    {
        var (vm, family, _, _) = Create(isFull: false);
        var before = family.Settings.ArrivalMinutes;

        vm.ArrivalTime = TimeSpan.FromHours(6);

        family.Settings.ArrivalMinutes.Should().Be(before);
    }

    [TestMethod]
    public async Task Buy_SuccessUnlocksEverythingAndSaysThanks()
    {
        var (vm, _, _, messages) = Create(isFull: false);
        vm.IsFree.Should().BeTrue();

        await vm.BuyCommand.ExecuteAsync(null);

        vm.IsFull.Should().BeTrue();
        vm.CanPurchase.Should().BeFalse();
        messages.Shown.Should().Be(1);
    }

    [TestMethod]
    public void AddEvent_SavesMonthAndDay()
    {
        var (vm, family, _, _) = Create();
        vm.NewEventName = "Birthday";
        vm.NewEventDate = new DateTimeOffset(new DateTime(2027, 3, 14));

        vm.AddEventCommand.Execute(null);

        family.Events.Should().ContainSingle(e => e.Name == "Birthday" && e.Month == 3 && e.Day == 14);
    }
}
