using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class StickerGroupTests
{
    private static WishListViewModel Create(FamilyService? family = null)
    {
        family ??= new(new FakeFamilyStore(), "en-US");
        FakeStringLocalizer localizer = new();
        return new(family, new FakeClock(new DateTime(2026, 11, 5)), localizer, new FakeGate(), new FakeShare(), new KidPickerViewModel(family, localizer), new FakeSounds());
    }

    [TestMethod]
    public void StickerGroups_FollowTheCatalogWithLocalizedNames()
    {
        var vm = Create();

        vm.StickerGroups.Select(g => g.Name).Should().Equal(StickerCatalog.Groups.Select(g => $"StickerGroup_{g.Key}"));
        vm.StickerGroups.SelectMany(g => g.Stickers).Select(s => s.Key).Should().Equal(StickerCatalog.Keys);
        vm.StickerGroups[0].Stickers[0].Name.Should().Be($"Sticker_{StickerCatalog.Groups[0].Stickers[0]}");
    }

    [TestMethod]
    public void Stickers_ListsEveryStickerOnce()
    {
        Create().Stickers.Select(s => s.Key).Should().Equal(StickerCatalog.Keys);
    }

    [TestMethod]
    public void AddWish_WithANewlyOfferedSticker_KeepsItsKey()
    {
        FamilyService family = new(new FakeFamilyStore(), "en-US");
        var vm = Create(family);
        var unicorn = vm.Stickers.Single(s => s.Key == "unicorn");

        vm.ChooseStickerCommand.Execute(unicorn);
        vm.AddWishCommand.Execute(null);

        vm.Wishes.Should().ContainSingle(w => w.Sticker == "unicorn" && w.Label == "Sticker_unicorn");
        family.ActiveKid.Wishes.Single().Sticker.Should().Be("unicorn");
    }

    [TestMethod]
    public void SelectedSticker_IsCleared_WhenTheSameStickerIsChosenAgain()
    {
        var vm = Create();
        var sticker = vm.Stickers[0];

        vm.ChooseStickerCommand.Execute(sticker);
        vm.ChooseStickerCommand.Execute(sticker);

        vm.SelectedSticker.Should().BeNull();
    }

    [TestMethod]
    public void ChooseSticker_MarksOnlyThePickedOneAsSelected()
    {
        var vm = Create();

        vm.ChooseStickerCommand.Execute(vm.Stickers[0]);
        vm.ChooseStickerCommand.Execute(vm.Stickers[1]);

        vm.Stickers.Where(s => s.IsSelected).Should().Equal(vm.Stickers[1]);
    }

    [TestMethod]
    public void AddWish_ClearsTheSelectedMark()
    {
        var vm = Create();
        vm.ChooseStickerCommand.Execute(vm.Stickers[0]);

        vm.AddWishCommand.Execute(null);

        vm.Stickers.Should().OnlyContain(s => !s.IsSelected);
    }
}
