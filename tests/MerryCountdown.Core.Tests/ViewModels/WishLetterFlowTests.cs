using FluentAssertions;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Services;
using MerryCountdown.Core.Tests.Fakes;
using MerryCountdown.Core.ViewModels;

namespace MerryCountdown.Core.Tests.ViewModels;

[TestClass]
public class WishLetterFlowTests
{
    private readonly FamilyService _family = new(new FakeFamilyStore(), "en-US");
    private readonly FakeSounds _sounds = new();

    private WishListViewModel Create()
    {
        FakeStringLocalizer localizer = new(new Dictionary<string, string> { ["WishRemoved"] = "{0} is gone" });
        return new(_family, new FakeClock(new DateTime(2026, 11, 5)), localizer, new FakeGate(), new FakeShare(), new KidPickerViewModel(_family, localizer), _sounds);
    }

    private static void Add(WishListViewModel vm, string text)
    {
        vm.NewWishText = text;
        vm.AddWishCommand.Execute(null);
    }

    [TestMethod]
    public void ChoosePicture_WaitsInsteadOfAddingTheWish()
    {
        var vm = Create();
        var teddy = vm.Stickers.Single(s => s.Key == "teddy");

        vm.ChoosePictureCommand.Execute(teddy);

        vm.Wishes.Should().BeEmpty();
        vm.SelectedSticker.Should().BeSameAs(teddy);
        teddy.IsSelected.Should().BeTrue();
        vm.AddWishCommand.CanExecute(null).Should().BeTrue();
    }

    [TestMethod]
    public void ChoosePicture_ItsNameBecomesThePlaceholder()
    {
        var vm = Create();
        vm.WishPlaceholder.Should().Be("WishPlaceholder");

        vm.ChoosePictureCommand.Execute(vm.Stickers.Single(s => s.Key == "teddy"));

        vm.WishPlaceholder.Should().Be("Sticker_teddy");
    }

    [TestMethod]
    public void ChoosePicture_AnotherOne_ReplacesTheFirst()
    {
        var vm = Create();
        var teddy = vm.Stickers.Single(s => s.Key == "teddy");

        vm.ChoosePictureCommand.Execute(teddy);
        vm.ChoosePictureCommand.Execute(vm.Stickers.Single(s => s.Key == "unicorn"));

        teddy.IsSelected.Should().BeFalse();
        vm.SelectedSticker!.Key.Should().Be("unicorn");
    }

    [TestMethod]
    public void AddWish_WithOnlyAPicture_UsesThePicturesOwnName()
    {
        var vm = Create();
        vm.ChoosePictureCommand.Execute(vm.Stickers.Single(s => s.Key == "teddy"));

        vm.AddWishCommand.Execute(null);

        vm.Wishes.Should().ContainSingle(w => w.Sticker == "teddy" && w.Label == "Sticker_teddy");
        _family.ActiveKid.Wishes.Single().Sticker.Should().Be("teddy");
    }

    [TestMethod]
    public void AddWish_WithAPictureAndWords_KeepsBothAndStartsAfresh()
    {
        var vm = Create();
        var teddy = vm.Stickers.Single(s => s.Key == "teddy");
        vm.ChoosePictureCommand.Execute(teddy);
        vm.NewWishText = "the brown one";

        vm.AddWishCommand.Execute(null);

        vm.Wishes.Should().ContainSingle(w => w.Sticker == "teddy" && w.Label == "the brown one");
        vm.NewWishText.Should().BeEmpty();
        vm.SelectedSticker.Should().BeNull();
        teddy.IsSelected.Should().BeFalse();
        vm.WishPlaceholder.Should().Be("WishPlaceholder");
    }

    [TestMethod]
    public void ChoosePicture_Pops()
    {
        var vm = Create();

        vm.ChoosePictureCommand.Execute(vm.Stickers[0]);

        _sounds.Played.Should().Equal(SoundEffect.Sticker);
    }

    [TestMethod]
    public void AddWish_NeedsText()
    {
        var vm = Create();

        vm.AddWishCommand.CanExecute(null).Should().BeFalse();

        vm.NewWishText = "Sledge";

        vm.AddWishCommand.CanExecute(null).Should().BeTrue();
    }

    [TestMethod]
    public void AddWish_MarksTheNewLineAsArrived()
    {
        var vm = Create();
        Add(vm, "Sledge");

        Add(vm, "Skates");

        vm.ArrivedWish.Should().BeSameAs(vm.Wishes[^1]);
        vm.ArrivedWish!.Label.Should().Be("Skates");
    }

    [TestMethod]
    public void ArrivedWish_IsClearedByRemoving()
    {
        var vm = Create();
        Add(vm, "Sledge");

        vm.RemoveWishCommand.Execute(vm.Wishes[0]);

        vm.ArrivedWish.Should().BeNull();
    }

    [TestMethod]
    public void RemoveWish_OffersToPutItBack()
    {
        var vm = Create();
        Add(vm, "Sledge");

        vm.RemoveWishCommand.Execute(vm.Wishes[0]);

        vm.CanUndoRemove.Should().BeTrue();
        vm.RemovedMessage.Should().Be("Sledge is gone");
    }

    [TestMethod]
    public void UndoRemove_PutsItBackInItsPlace()
    {
        var vm = Create();
        Add(vm, "Sledge");
        Add(vm, "Puppy");
        Add(vm, "Skates");

        vm.RemoveWishCommand.Execute(vm.Wishes[1]);
        vm.UndoRemoveCommand.Execute(null);

        vm.Wishes.Select(w => w.Label).Should().Equal("Sledge", "Puppy", "Skates");
        _family.ActiveKid.Wishes.Select(w => w.Text).Should().Equal("Sledge", "Puppy", "Skates");
        vm.CanUndoRemove.Should().BeFalse();
        vm.ArrivedWish!.Label.Should().Be("Puppy");
    }

    [TestMethod]
    public void AddingAnotherWish_DropsTheUndo()
    {
        var vm = Create();
        Add(vm, "Sledge");
        vm.RemoveWishCommand.Execute(vm.Wishes[0]);

        Add(vm, "Puppy");

        vm.CanUndoRemove.Should().BeFalse();
    }

    [TestMethod]
    public void SwitchingKid_DropsTheUndo()
    {
        var vm = Create();
        vm.ViewLoaded();
        Add(vm, "Sledge");
        vm.RemoveWishCommand.Execute(vm.Wishes[0]);

        _family.SetActiveKid(_family.AddKid("Ema"));
        vm.UndoRemoveCommand.Execute(null);

        _family.ActiveKid.Wishes.Should().BeEmpty();
        vm.CanUndoRemove.Should().BeFalse();
    }

    [TestMethod]
    public void SendHint_AsksForAWishFirst()
    {
        var vm = Create();
        vm.KidName = "Anna";

        vm.SendHint.Should().Be("SendNeedsWish");
        vm.CanSend.Should().BeFalse();
    }

    [TestMethod]
    public void SendHint_AsksForTheName_OnceThereIsAWish()
    {
        var vm = Create();
        Add(vm, "Sledge");

        vm.SendHint.Should().Be("SendNeedsName");
        vm.CanSend.Should().BeFalse();
    }

    [TestMethod]
    public void SendHint_SaysReady_WithAWishAndAName()
    {
        var vm = Create();
        Add(vm, "Sledge");

        vm.KidName = "Anna";

        vm.SendHint.Should().Be("SendReady");
        vm.CanSend.Should().BeTrue();
    }

    [TestMethod]
    public void SendHint_ABlankNameIsNoName()
    {
        var vm = Create();
        Add(vm, "Sledge");

        vm.KidName = "   ";

        vm.SendHint.Should().Be("SendNeedsName");
    }

    [TestMethod]
    public void SendLetter_WithoutAName_DoesNothing()
    {
        var vm = Create();
        Add(vm, "Sledge");

        vm.SendLetterCommand.Execute(null);

        vm.IsSent.Should().BeFalse();
        _family.ActiveKid.LetterSentYear.Should().BeNull();
    }

    [TestMethod]
    public void SendLetter_DropsTheUndo()
    {
        var vm = Create();
        vm.KidName = "Anna";
        Add(vm, "Sledge");
        Add(vm, "Puppy");
        vm.RemoveWishCommand.Execute(vm.Wishes[1]);

        vm.SendLetterCommand.Execute(null);

        vm.CanUndoRemove.Should().BeFalse();
    }

    [TestMethod]
    public void IsEmpty_UntilTheFirstWish()
    {
        var vm = Create();
        vm.IsEmpty.Should().BeTrue();

        Add(vm, "Sledge");

        vm.IsEmpty.Should().BeFalse();
    }

    [TestMethod]
    public void CalmMotion_FollowsTheSetting()
    {
        _family.Settings.CalmMotion = true;

        Create().CalmMotion.Should().BeTrue();
    }
}
