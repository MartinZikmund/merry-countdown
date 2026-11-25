using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Tests.Fakes;

namespace MerryCountdown.Core.Tests;

[TestClass]
public class FamilyServiceTests
{
    private static FamilyService LoadVersion1(Action<FamilySettings> setUp)
    {
        FamilySettings settings = FamilySettings.ForCulture("en-US");
        settings.SceneId = "globe";
        setUp(settings);
        return new FamilyService(new FakeFamilyStore { Data = new FamilyData { Version = 1, Settings = settings } }, "en-US");
    }

    [TestMethod]
    public void Version1_JustThePhoto_BecomesThePhotosScene()
    {
        var family = LoadVersion1(s => s.UsesPhotos = true);

        family.Settings.SceneId.Should().Be(SceneCatalog.PhotosId);
        family.Settings.UsesPhotos.Should().BeTrue();
    }

    [TestMethod]
    public void Version1_PhotosWithASceneInFront_KeepsThatSceneInFront()
    {
        var family = LoadVersion1(s =>
        {
            s.UsesPhotos = true;
            s.PhotoForegroundSceneId = "fireplace";
        });

        family.Settings.SceneId.Should().Be("fireplace");
        family.Settings.UsesPhotos.Should().BeTrue();
        family.Settings.PhotoForegroundSceneId.Should().BeNull();
    }

    [TestMethod]
    public void Version1_PhotosOff_KeepsItsSceneWhateverTheForegroundWas()
    {
        var family = LoadVersion1(s => s.PhotoForegroundSceneId = "fireplace");

        family.Settings.SceneId.Should().Be("globe");
        family.Settings.UsesPhotos.Should().BeFalse();
    }

    [TestMethod]
    public void CurrentVersion_IsNotMigratedAgain()
    {
        FamilySettings settings = FamilySettings.ForCulture("en-US");
        settings.SceneId = "globe";
        settings.UsesPhotos = true;

        FamilyService family = new(new FakeFamilyStore { Data = new FamilyData { Settings = settings } }, "en-US");

        family.Settings.SceneId.Should().Be("globe");
    }

    private sealed class FakeStore : IFamilyStore
    {
        public FamilyData Data { get; set; } = new();

        public int Saves { get; private set; }

        public FamilyData Load() => Data;

        public void Save(FamilyData data)
        {
            Data = data;
            Saves++;
        }
    }

    private static (FamilyService Service, FakeStore Store) Create()
    {
        FakeStore store = new();
        return (new FamilyService(store, "en-US"), store);
    }

    [TestMethod]
    public void NewFamily_StartsWithOneKidWhoIsActive()
    {
        var (service, _) = Create();

        service.Kids.Should().HaveCount(1);
        service.ActiveKid.Should().BeSameAs(service.Kids[0]);
    }

    [TestMethod]
    public void NewFamily_TakesItsTraditionFromTheCulture()
    {
        new FamilyService(new FakeStore(), "cs-CZ").Settings.GiftBringer.Should().Be(GiftBringer.Jezisek);
    }

    [TestMethod]
    public void AddWish_GoesOnlyOnTheActiveKidsList()
    {
        var (service, _) = Create();
        var first = service.ActiveKid;
        var second = service.AddKid("Ema");

        service.SetActiveKid(second);
        service.AddWish("A red bike", "bike");

        second.Wishes.Should().ContainSingle(w => w.Text == "A red bike");
        first.Wishes.Should().BeEmpty();
    }

    [TestMethod]
    public void AddWish_TrimsAndIgnoresBlankWithoutASticker()
    {
        var (service, _) = Create();

        service.AddWish("   ", null).Should().BeNull();
        service.AddWish("  puppy  ", null)!.Text.Should().Be("puppy");
    }

    [TestMethod]
    public void AddWish_StickerAloneIsEnoughForKidsWhoCannotWriteYet()
    {
        var (service, _) = Create();

        service.AddWish("", "teddy").Should().NotBeNull();
    }

    [TestMethod]
    public void AddWish_AfterLetterWasSent_ReopensItForAPostscript()
    {
        var (service, _) = Create();
        service.AddWish("puppy", null);
        service.SendLetter(2026);

        service.AddWish("kitten", null);

        service.ActiveKid.LetterSentYear.Should().BeNull();
    }

    [TestMethod]
    public void RestoreWish_PutsItBackWhereItWas()
    {
        var (service, store) = Create();
        service.AddWish("sledge", null);
        var puppy = service.AddWish("puppy", null)!;
        service.AddWish("skates", null);
        service.RemoveWish(puppy);
        var saves = store.Saves;

        service.RestoreWish(puppy, 1);

        service.ActiveKid.Wishes.Select(w => w.Text).Should().Equal("sledge", "puppy", "skates");
        store.Saves.Should().Be(saves + 1);
    }

    [TestMethod]
    public void RestoreWish_PastTheEnd_GoesLast()
    {
        var (service, _) = Create();
        service.AddWish("sledge", null);

        service.RestoreWish(new Wish { Text = "puppy" }, 5);

        service.ActiveKid.Wishes.Select(w => w.Text).Should().Equal("sledge", "puppy");
    }

    [TestMethod]
    public void SendLetter_WithNoWishes_DoesNothing()
    {
        var (service, _) = Create();

        service.SendLetter(2026).Should().BeFalse();

        service.ActiveKid.LetterSentYear.Should().BeNull();
    }

    [TestMethod]
    public void RemoveWish_RemovesItFromTheActiveKid()
    {
        var (service, _) = Create();
        var wish = service.AddWish("puppy", null)!;

        service.RemoveWish(wish);

        service.ActiveKid.Wishes.Should().BeEmpty();
    }

    [TestMethod]
    public void RemoveKid_NeverRemovesTheLastOne()
    {
        var (service, _) = Create();

        service.RemoveKid(service.ActiveKid).Should().BeFalse();

        service.Kids.Should().HaveCount(1);
    }

    [TestMethod]
    public void RemoveKid_Active_MovesActiveToAnotherKid()
    {
        var (service, _) = Create();
        var first = service.ActiveKid;
        var second = service.AddKid("Ema");
        service.SetActiveKid(second);

        service.RemoveKid(second).Should().BeTrue();

        service.ActiveKid.Should().BeSameAs(first);
    }

    [TestMethod]
    public void OpenDoor_IsTrackedPerKidAndPerYear()
    {
        var (service, _) = Create();
        var first = service.ActiveKid;
        var second = service.AddKid("Ema");

        service.OpenDoor(first, 2026, 3);

        service.IsDoorOpened(first, 2026, 3).Should().BeTrue();
        service.IsDoorOpened(second, 2026, 3).Should().BeFalse();
        service.IsDoorOpened(first, 2027, 3).Should().BeFalse();
    }

    [TestMethod]
    public void DoorMessage_BelongsToOneKidAndDay()
    {
        var (service, _) = Create();
        var kid = service.ActiveKid;

        service.SetDoorMessage(kid, 5, "Look under your pillow!");

        kid.DoorMessages[5].Should().Be("Look under your pillow!");

        service.SetDoorMessage(kid, 5, "  ");

        kid.DoorMessages.Should().NotContainKey(5);
    }

    [TestMethod]
    public void Changes_ArePersistedAndRaiseChanged()
    {
        var (service, store) = Create();
        var raised = 0;
        service.Changed += (_, _) => raised++;

        service.AddKid("Ema");

        raised.Should().Be(1);
        store.Saves.Should().Be(1);
    }

    [TestMethod]
    public void Load_DamagedActiveKidId_FallsBackToFirstKid()
    {
        FakeStore store = new() { Data = new FamilyData { ActiveKidId = "nope", Kids = [new Kid { Name = "Tom" }] } };

        new FamilyService(store, "en-US").ActiveKid.Name.Should().Be("Tom");
    }

    [TestMethod]
    public void WishesAsText_ListsTheKidsWishesForGrownUps()
    {
        var (service, _) = Create();
        service.ActiveKid.Name = "Ema";
        service.AddWish("puppy", null);
        service.AddWish("", "teddy");

        var text = service.ActiveKid.WishesAsText(sticker => sticker == "teddy" ? "Teddy bear" : sticker);

        text.Should().Contain("Ema").And.Contain("puppy").And.Contain("Teddy bear");
    }
}
