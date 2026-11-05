using System.Text.Json;
using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class CountdownSignViewModelTests
{
    private static CountdownSignViewModel Update(DateTime now, FamilySettings? settings = null)
    {
        CountdownSignViewModel sign = new(new FakeStringLocalizer());
        sign.Update(now, settings ?? Family(BigDay.ChristmasDay));
        return sign;
    }

    private static FamilySettings Family(BigDay bigDay)
    {
        FamilySettings settings = new();
        settings.UseBigDay(bigDay);
        return settings;
    }

    [TestMethod]
    public void FamilySettings_Default_IsPlankWithNumberPlates()
    {
        FamilySettings settings = new();

        settings.Sign.Should().Be(SignDesign.Plank);
        settings.CountdownStyle.Should().Be(CountdownStyle.Plates);
    }

    [TestMethod]
    public void FamilySettings_SavedBeforeSignsExisted_LoadWithTheDefaults()
    {
        var data = JsonSerializer.Deserialize("""{"Settings":{"SceneId":"globe"}}""", FamilyJsonContext.Default.FamilyData);

        data!.Settings!.Sign.Should().Be(SignDesign.Plank);
        data.Settings.CountdownStyle.Should().Be(CountdownStyle.Plates);
    }

    [TestMethod]
    public void Update_PadsPlatesToTwoDigits_AndLetsDaysGrow()
    {
        var soon = Update(new DateTime(2026, 12, 20, 23, 0, 0));
        var far = Update(new DateTime(2026, 1, 10));

        soon.Days.Should().Be("04");
        soon.Hours.Should().Be("01");
        far.Days.Should().Be("349");
    }

    [TestMethod]
    public void Update_EveningArrival_ClockCountsToMidnightNotToTheArrival()
    {
        var sign = Update(new DateTime(2026, 12, 9, 18, 30, 12), Family(BigDay.ChristmasEve));

        sign.SleepsNumber.Should().Be("15");
        (sign.ClockHours, sign.ClockMinutes, sign.ClockSeconds).Should().Be(("05", "29", "48"));
        (sign.Days, sign.Hours).Should().Be(("14", "22"));
    }

    [DataRow(1, "UnitDays_One")]
    [DataRow(2, "UnitDays_Few")]
    [DataRow(5, "UnitDays_Many")]
    [TestMethod]
    public void Update_LabelsArePluralized(int days, string expected)
    {
        var sign = Update(new DateTime(2026, 12, 25).AddDays(-days));

        sign.DaysLabel.Should().Be(expected);
    }

    [TestMethod]
    public void Update_WithSleepsLeft_CaptionNamesTheNextNumber()
    {
        Update(new DateTime(2026, 12, 20), Family(BigDay.ChristmasDay)).NextChangeCaption.Should().Be("SignNextChange");
    }

    [TestMethod]
    public void Update_OnTheLastSleep_CaptionNamesTheBigDay()
    {
        Update(new DateTime(2026, 12, 24, 12, 0, 0), Family(BigDay.ChristmasDay)).NextChangeCaption.Should().Be("SignNextChangeLastSleep");
    }

    [TestMethod]
    public void Update_OnTheBigDayBeforeArrival_CaptionPromisesThePresents()
    {
        Update(new DateTime(2026, 12, 24, 10, 0, 0), Family(BigDay.ChristmasEve)).NextChangeCaption.Should().Be("SignNextChangeBigDay");
    }

    [TestMethod]
    public void Update_AccessibleName_DoesNotChangeEverySecond()
    {
        var sign = Update(new DateTime(2026, 12, 9, 12, 0, 10));
        List<string> changes = [];
        sign.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CountdownSignViewModel.AccessibleName))
            {
                changes.Add(sign.AccessibleName);
            }
        };

        sign.Update(new DateTime(2026, 12, 9, 12, 0, 11), Family(BigDay.ChristmasDay));

        changes.Should().BeEmpty();
    }

    [TestMethod]
    public void Update_CarriesTheFamilysChoices()
    {
        var sign = Update(new DateTime(2026, 12, 9), new FamilySettings { Sign = SignDesign.Baubles, CountdownStyle = CountdownStyle.SleepsAndClock });

        sign.Design.Should().Be(SignDesign.Baubles);
        sign.Style.Should().Be(CountdownStyle.SleepsAndClock);
    }
}

[TestClass]
public class HomeChoicesAndDoorTests
{
    private static (CountdownViewModel Vm, FamilyService Family, FakeNavigation Nav) Create(DateTime now, FakeStringLocalizer? localizer = null)
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeTicker ticker = new();
        FakeNavigation nav = new();
        FakeFullVersion full = new();
        return (new CountdownViewModel(family, full, new FakeClock(now), ticker, localizer ?? new FakeStringLocalizer(), nav, new FakeDisplayMode(), new PhotoBackdrop(family, full, new FakePhotoLibrary(), ticker), new FakeGate(), new FakeSounds()), family, nav);
    }

    [TestMethod]
    public void SignOptions_OfferAllFive_WithTheChosenOneSelected()
    {
        var (vm, _, _) = Create(new DateTime(2026, 11, 5));

        vm.SignOptions.Select(o => o.Design).Should().BeEquivalentTo(Enum.GetValues<SignDesign>());
        vm.SignOptions.Single(o => o.IsSelected).Design.Should().Be(SignDesign.Plank);
    }

    [TestMethod]
    public void SelectSign_SavesAndMovesTheSelection()
    {
        var (vm, family, _) = Create(new DateTime(2026, 11, 5));
        vm.ViewLoaded();

        vm.SelectSignCommand.Execute(vm.SignOptions.Single(o => o.Design == SignDesign.Gingerbread));

        family.Settings.Sign.Should().Be(SignDesign.Gingerbread);
        vm.Sign.Design.Should().Be(SignDesign.Gingerbread);
        vm.SignOptions.Single(o => o.IsSelected).Design.Should().Be(SignDesign.Gingerbread);
    }

    [TestMethod]
    public void SelectStyle_SavesAndMovesTheSelection()
    {
        var (vm, family, _) = Create(new DateTime(2026, 11, 5));
        vm.ViewLoaded();

        vm.SelectStyleCommand.Execute(vm.StyleOptions.Single(o => o.Style == CountdownStyle.SleepsAndClock));

        family.Settings.CountdownStyle.Should().Be(CountdownStyle.SleepsAndClock);
        vm.Sign.Style.Should().Be(CountdownStyle.SleepsAndClock);
        vm.StyleOptions.Single(o => o.IsSelected).Style.Should().Be(CountdownStyle.SleepsAndClock);
    }

    [TestMethod]
    public void Door_InNovember_IsLockedAndCountsToTheFirstDoor()
    {
        var (vm, _, _) = Create(new DateTime(2026, 11, 5));

        vm.DoorStage.Should().Be(HomeDoorStage.Locked);
        vm.DoorNumber.Should().Be(1);
        vm.DoorCaption.Should().Be("DoorOneOpensIn_Many");
        vm.CanWriteWishLetter.Should().BeTrue();
    }

    [DataRow(2026, 12, 27, 339)]
    [DataRow(2027, 1, 3, 332)]
    [TestMethod]
    public void Door_AfterChristmas_CountsToNextYearsFirstDoor(int year, int month, int day, int sleeps)
    {
        FakeStringLocalizer localizer = new(new Dictionary<string, string> { ["DoorOneOpensIn_Many"] = "{0}" });

        var (vm, _, _) = Create(new DateTime(year, month, day), localizer);

        vm.DoorStage.Should().Be(HomeDoorStage.Locked);
        vm.DoorCaption.Should().Be(sleeps.ToString());
    }

    [TestMethod]
    public void Door_InDecember_WaitsUntilThisKidOpensIt()
    {
        var (vm, family, _) = Create(new DateTime(2026, 12, 5));

        vm.DoorStage.Should().Be(HomeDoorStage.Waiting);
        vm.DoorNumber.Should().Be(5);
        vm.DoorCaption.Should().Be("OpenDoorButton");
        vm.CanWriteWishLetter.Should().BeFalse();

        family.OpenDoor(family.ActiveKid, 2026, 5);
        vm.Refresh();

        vm.DoorStage.Should().Be(HomeDoorStage.Opened);
        vm.DoorCaption.Should().Be("DoorSeeYouTomorrow");
    }

    [TestMethod]
    public void Door_LastDoorOpened_DoesNotPromiseAnotherTomorrow()
    {
        var (vm, family, _) = Create(new DateTime(2026, 12, 24, 9, 0, 0));

        family.OpenDoor(family.ActiveKid, 2026, 24);
        vm.Refresh();

        vm.DoorStage.Should().Be(HomeDoorStage.Opened);
        vm.DoorCaption.Should().BeEmpty();
    }

    [TestMethod]
    public void Door_OnChristmasDay_StepsAside()
    {
        var (vm, _, _) = Create(new DateTime(2026, 12, 25, 8, 0, 0));

        vm.DoorStage.Should().Be(HomeDoorStage.Hidden);
        vm.CanWriteWishLetter.Should().BeFalse();
    }

    [DataRow("train", true)]
    [DataRow("village", false)]
    [TestMethod]
    public void IsDoorOnRight_OnlyWhereTheTrainRunsAlongTheLeft(string scene, bool expected)
    {
        var (vm, _, _) = Create(new DateTime(2026, 12, 5));

        vm.SceneId = scene;

        vm.IsDoorOnRight.Should().Be(expected);
    }

    [TestMethod]
    public void OpenTodaysDoor_GoesToThatDoorsReveal()
    {
        var (vm, _, nav) = Create(new DateTime(2026, 12, 5));

        vm.OpenTodaysDoorCommand.Execute(null);

        nav.Visited.Should().Contain(typeof(CalendarViewModel));
        nav.LastParameter.Should().Be(5);
    }

    [TestMethod]
    public void WriteWishLetter_GoesToTheWishList()
    {
        var (vm, _, nav) = Create(new DateTime(2026, 11, 5));

        vm.WriteWishLetterCommand.Execute(null);

        nav.Visited.Should().Contain(typeof(WishListViewModel));
    }

    [TestMethod]
    public void Display_FollowsTheFamilysSign()
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeTicker ticker = new();
        FakeFullVersion full = new(true);
        family.Settings.Sign = SignDesign.Station;
        family.Settings.CountdownStyle = CountdownStyle.SleepsAndClock;

        DisplayViewModel vm = new(family, full, new FakeClock(new DateTime(2026, 12, 9)), ticker, new FakeStringLocalizer(), new FakeDisplayMode(), new PhotoBackdrop(family, full, new FakePhotoLibrary(), ticker), new FakeSounds());

        vm.Sign.Design.Should().Be(SignDesign.Station);
        vm.Sign.Style.Should().Be(CountdownStyle.SleepsAndClock);
    }
}
