using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class PhotoBackdropTests
{
    private static (PhotoBackdrop Backdrop, FamilyService Family, FakeFullVersion Full, FakeTicker Ticker, FakePhotoLibrary Library) Create(bool isFull = true, params string[] photos)
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeFullVersion full = new(isFull);
        FakeTicker ticker = new();
        FakePhotoLibrary library = new();
        foreach (var photo in photos)
        {
            library.Seed(photo);
            family.Settings.PhotoIds.Add(photo);
        }

        family.Settings.UsesPhotos = photos.Length > 0;
        return (new PhotoBackdrop(family, full, library, ticker), family, full, ticker, library);
    }

    private static void Tick(FakeTicker ticker, int seconds)
    {
        for (var i = 0; i < seconds; i++)
        {
            ticker.Fire();
        }
    }

    [TestMethod]
    public void NoPhotos_IsInactive()
    {
        var (backdrop, _, _, _, _) = Create();

        backdrop.IsActive.Should().BeFalse();
        backdrop.Photo.Should().BeNull();
        backdrop.ShowForeground.Should().BeTrue();
    }

    [TestMethod]
    public void WithPhotos_ShowsTheFirst()
    {
        var (backdrop, _, _, _, library) = Create(true, "a", "b");

        backdrop.IsActive.Should().BeTrue();
        backdrop.Photo.Should().BeSameAs(library.Bytes("a"));
    }

    [TestMethod]
    public void FreeVersion_NeverShowsPhotos()
    {
        var (backdrop, _, _, _, _) = Create(false, "a");

        backdrop.IsActive.Should().BeFalse();
        backdrop.Photo.Should().BeNull();
    }

    [TestMethod]
    public void Buying_TurnsPhotosOn_AndRefundTurnsThemOffWithoutLosingThem()
    {
        var (backdrop, family, full, _, _) = Create(false, "a");

        full.PurchaseAsync().Wait();
        backdrop.IsActive.Should().BeTrue();

        full.Revoke();
        backdrop.IsActive.Should().BeFalse();
        family.Settings.PhotoIds.Should().ContainSingle();
    }

    [TestMethod]
    public void Slideshow_ChangesAtTheChosenIntervalAndWraps()
    {
        var (backdrop, family, _, ticker, library) = Create(true, "a", "b");
        family.Settings.PhotoIntervalSeconds = 30;

        Tick(ticker, 29);
        backdrop.Photo.Should().BeSameAs(library.Bytes("a"));

        Tick(ticker, 1);
        backdrop.Photo.Should().BeSameAs(library.Bytes("b"));

        Tick(ticker, 30);
        backdrop.Photo.Should().BeSameAs(library.Bytes("a"));
    }

    [TestMethod]
    public void Next_RestartsTheInterval()
    {
        var (backdrop, family, _, ticker, library) = Create(true, "a", "b");
        family.Settings.PhotoIntervalSeconds = 30;
        Tick(ticker, 29);

        backdrop.Next();
        Tick(ticker, 29);

        backdrop.Photo.Should().BeSameAs(library.Bytes("b"));
    }

    [TestMethod]
    public void OnePhoto_DoesNotReloadItAtEveryInterval()
    {
        var (backdrop, family, _, ticker, library) = Create(true, "a");
        family.Settings.PhotoIntervalSeconds = 30;
        var loads = library.Loads;

        Tick(ticker, 90);

        library.Loads.Should().Be(loads);
    }

    [TestMethod]
    public void MissingFile_IsSkipped()
    {
        var (backdrop, family, _, ticker, library) = Create(true, "a", "b", "c");
        family.Settings.PhotoIntervalSeconds = 30;
        library.Lose("b");

        Tick(ticker, 30);

        backdrop.Photo.Should().BeSameAs(library.Bytes("c"));
    }

    [TestMethod]
    public void AllFilesMissing_FallsBackToTheScene()
    {
        var (backdrop, family, _, _, library) = Create(false, "a");
        library.Lose("a");

        family.SaveSettings();

        backdrop.Photo.Should().BeNull();
        backdrop.ShowForeground.Should().BeTrue();
    }

    [TestMethod]
    public void RemovingTheShownPhoto_MovesOnToAnother()
    {
        var (backdrop, family, _, _, library) = Create(true, "a", "b");

        family.Settings.PhotoIds.Remove("a");
        family.SaveSettings();

        backdrop.Photo.Should().BeSameAs(library.Bytes("b"));
    }

    [TestMethod]
    public void Foreground_FollowsTheChoiceWhilePhotosAreOn()
    {
        var (backdrop, family, _, _, _) = Create(true, "a");
        backdrop.ShowForeground.Should().BeFalse();

        family.Settings.PhotoForegroundSceneId = "fireplace";
        family.SaveSettings();

        backdrop.ShowForeground.Should().BeTrue();
        backdrop.SceneFor("village").Should().Be("fireplace");
    }

    [TestMethod]
    public void SceneFor_WithoutPhotos_KeepsTheChosenScene()
    {
        var (backdrop, family, _, _, _) = Create();
        family.Settings.PhotoForegroundSceneId = "fireplace";

        backdrop.SceneFor("village").Should().Be("village");
    }
}

[TestClass]
public class PhotoFeatureViewModelTests
{
    private static (ParentZoneViewModel Vm, FamilyService Family, FakePhotoLibrary Library, FakeMessages Messages) CreateParentZone(bool isFull = true)
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakePhotoLibrary library = new();
        FakeMessages messages = new();
        return (new ParentZoneViewModel(family, new FakeFullVersion(isFull), new FakeStringLocalizer(), messages, new FakeClock(new DateTime(2026, 11, 5)), library), family, library, messages);
    }

    [TestMethod]
    public async Task AddPhotos_FirstOnesTurnThePhotosOn()
    {
        var (vm, family, library, _) = CreateParentZone();
        library.NextPick = ["a", "b"];

        await vm.AddPhotosCommand.ExecuteAsync(null);

        family.Settings.PhotoIds.Should().Equal("a", "b");
        family.Settings.UsesPhotos.Should().BeTrue();
        vm.UsesPhotos.Should().BeTrue();
        vm.Photos.Should().HaveCount(2);
    }

    [TestMethod]
    public async Task AddPhotos_CancelledPicker_ChangesNothing()
    {
        var (vm, family, _, messages) = CreateParentZone();

        await vm.AddPhotosCommand.ExecuteAsync(null);

        family.Settings.UsesPhotos.Should().BeFalse();
        messages.Shown.Should().Be(0);
    }

    [TestMethod]
    public async Task AddPhotos_UnreadableFiles_SaySo()
    {
        var (vm, _, library, messages) = CreateParentZone();
        library.NextFailures = 1;

        await vm.AddPhotosCommand.ExecuteAsync(null);

        messages.Shown.Should().Be(1);
    }

    [TestMethod]
    public async Task RemovePhoto_DeletesItAndTheLastOneTurnsPhotosOff()
    {
        var (vm, family, library, _) = CreateParentZone();
        library.NextPick = ["a"];
        await vm.AddPhotosCommand.ExecuteAsync(null);

        await vm.RemovePhotoCommand.ExecuteAsync(vm.Photos[0]);

        library.Deleted.Should().Contain("a");
        family.Settings.PhotoIds.Should().BeEmpty();
        family.Settings.UsesPhotos.Should().BeFalse();
        vm.Photos.Should().BeEmpty();
    }

    [TestMethod]
    public async Task RemovePhoto_AsksFirst()
    {
        var (vm, family, library, messages) = CreateParentZone();
        library.NextPick = ["a"];
        await vm.AddPhotosCommand.ExecuteAsync(null);
        messages.ConfirmResult = false;

        await vm.RemovePhotoCommand.ExecuteAsync(vm.Photos[0]);

        family.Settings.PhotoIds.Should().ContainSingle();
    }

    [TestMethod]
    public void Interval_SavesTheChosenSeconds()
    {
        var (vm, family, _, _) = CreateParentZone();

        vm.PhotoIntervalIndex = 0;

        family.Settings.PhotoIntervalSeconds.Should().Be(ParentZoneViewModel.PhotoIntervals[0]);
    }

    [TestMethod]
    public void Interval_DefaultsToFiveMinutes()
    {
        var (vm, _, _, _) = CreateParentZone();

        ParentZoneViewModel.PhotoIntervals[vm.PhotoIntervalIndex].Should().Be(300);
    }

    [TestMethod]
    public void Foreground_FirstOptionIsNone_RestAreScenes()
    {
        var (vm, family, _, _) = CreateParentZone();

        vm.ForegroundOptions.Should().HaveCount(SceneCatalog.All.Count + 1);
        vm.ForegroundIndex.Should().Be(0);

        vm.ForegroundIndex = 1;
        family.Settings.PhotoForegroundSceneId.Should().Be(SceneCatalog.All[0].Id);

        vm.ForegroundIndex = 0;
        family.Settings.PhotoForegroundSceneId.Should().BeNull();
    }

    private static (CountdownViewModel Vm, FamilyService Family, FakeNavigation Nav, FakeGate Gate) CreateCountdown(bool isFull, params string[] photos)
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeTicker ticker = new();
        FakePhotoLibrary library = new();
        foreach (var photo in photos)
        {
            library.Seed(photo);
            family.Settings.PhotoIds.Add(photo);
        }

        FakeFullVersion full = new(isFull);
        FakeNavigation nav = new();
        FakeGate gate = new();
        return (new CountdownViewModel(family, full, new FakeClock(new DateTime(2026, 11, 5)), ticker, new FakeStringLocalizer(), nav, new FakeDisplayMode(), new PhotoBackdrop(family, full, library, ticker), gate, new FakeSounds(), new FakeCompactOverlay(), new FakeMessages(), new FakeShakes()), family, nav, gate);
    }

    private static SceneOptionViewModel PhotosOption(CountdownViewModel vm) => vm.Scenes.Single(s => s.Id == CountdownViewModel.PhotosOptionId);

    [TestMethod]
    public void SceneMenu_MyPhotos_IsLockedInTheFreeVersion()
    {
        PhotosOption(CreateCountdown(false).Vm).IsLocked.Should().BeTrue();
        PhotosOption(CreateCountdown(true).Vm).IsLocked.Should().BeFalse();
    }

    [TestMethod]
    public async Task SceneMenu_MyPhotos_WithPhotos_TurnsThemOn()
    {
        var (vm, family, nav, _) = CreateCountdown(true, "a");

        await vm.SelectSceneCommand.ExecuteAsync(PhotosOption(vm));

        family.Settings.UsesPhotos.Should().BeTrue();
        nav.Visited.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SceneMenu_MyPhotos_WithNoneYet_GoesToTheGrownUpsThroughTheGate()
    {
        var (vm, _, nav, gate) = CreateCountdown(true);

        await vm.SelectSceneCommand.ExecuteAsync(PhotosOption(vm));

        gate.Asked.Should().Be(1);
        nav.Visited.Should().Contain(typeof(ParentZoneViewModel));
    }

    [TestMethod]
    public async Task SceneMenu_PickingAScene_TurnsPhotosOff()
    {
        var (vm, family, _, _) = CreateCountdown(true, "a");
        family.Settings.UsesPhotos = true;
        vm.ViewLoaded();

        await vm.SelectSceneCommand.ExecuteAsync(vm.Scenes.First(s => s.Id == "globe"));

        family.Settings.UsesPhotos.Should().BeFalse();
        vm.SceneId.Should().Be("globe");
    }

    [TestMethod]
    public void FamilyDisplay_NextMovesThroughPhotosInsteadOfScenes()
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeTicker ticker = new();
        FakePhotoLibrary library = new();
        library.Seed("a");
        library.Seed("b");
        family.Settings.PhotoIds.AddRange(["a", "b"]);
        family.Settings.UsesPhotos = true;
        FakeFullVersion full = new(true);
        PhotoBackdrop backdrop = new(family, full, library, ticker);
        DisplayViewModel vm = new(family, full, new FakeClock(new DateTime(2026, 11, 5)), ticker, new FakeStringLocalizer(), new FakeDisplayMode(), backdrop, new FakeSounds());
        var scene = vm.SceneId;

        vm.NextScene();

        backdrop.Photo.Should().BeSameAs(library.Bytes("b"));
        vm.SceneId.Should().Be(scene);
    }

    [TestMethod]
    public void FamilyDisplay_PhotosTakeOverFromSceneRotation()
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        FakeTicker ticker = new();
        FakePhotoLibrary library = new();
        library.Seed("a");
        family.Settings.PhotoIds.Add("a");
        family.Settings.UsesPhotos = true;
        FakeFullVersion full = new(true);
        DisplayViewModel vm = new(family, full, new FakeClock(new DateTime(2026, 11, 5)), ticker, new FakeStringLocalizer(), new FakeDisplayMode(), new PhotoBackdrop(family, full, library, ticker), new FakeSounds());
        vm.Start();
        var scene = vm.SceneId;

        for (var i = 0; i < 1800; i++)
        {
            ticker.Fire();
        }

        vm.SceneId.Should().Be(scene);
    }
}
