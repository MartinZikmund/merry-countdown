using System.Collections.ObjectModel;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Parents;
using MerryCountdown.Core.Season;

namespace MerryCountdown.Core.ViewModels;

/// <summary>Each kid's own letter to Santa or Ježíšek. Nothing here ever leaves the device unless a grown-up shares it.</summary>
public partial class WishListViewModel : ViewModelBase
{
    private const int MaxWishLength = 120;

    private readonly IFamilyService _family;
    private readonly IAppClock _clock;
    private readonly IStringLocalizer _localizer;
    private readonly IParentGateService _gate;
    private readonly IShareService _share;
    private Kid? _shownKid;
    private bool _isLoading;

    public WishListViewModel(
        IFamilyService family,
        IAppClock clock,
        IStringLocalizer localizer,
        IParentGateService gate,
        IShareService share,
        KidPickerViewModel kidPicker)
    {
        _family = family;
        _clock = clock;
        _localizer = localizer;
        _gate = gate;
        _share = share;
        KidPicker = kidPicker;
        PageTitle = localizer["NavWishes"];

        foreach (var key in WishStickers)
        {
            Stickers.Add(new WishStickerViewModel(key, localizer[$"Sticker_{key}"]));
        }

        Load();
    }

    /// <summary>Picture wishes, so kids who can't write yet can still ask for a puppy.</summary>
    public static IReadOnlyList<string> WishStickers { get; } =
        ["teddy", "bike", "ball", "game", "puppy", "kitten", "bricks", "train", "robot", "crown", "book", "paints", "skates", "candy"];

    public KidPickerViewModel KidPicker { get; }

    public int WishMaxLength => MaxWishLength;

    public ObservableCollection<WishItemViewModel> Wishes { get; } = [];

    public ObservableCollection<WishStickerViewModel> Stickers { get; } = [];

    [ObservableProperty]
    public partial string Salutation { get; set; } = "";

    [ObservableProperty]
    public partial string KidName { get; set; } = "";

    [ObservableProperty]
    public partial string SentMessage { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddWishCommand))]
    public partial string NewWishText { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddWishCommand))]
    public partial WishStickerViewModel? SelectedSticker { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWriting))]
    public partial bool IsSent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    public partial bool HasWishes { get; set; }

    public bool IsWriting => !IsSent;

    public bool CanSend => HasWishes;

    public override void ViewLoaded()
    {
        _family.Changed += OnFamilyChanged;
        Load();
    }

    public override void ViewUnloaded() => _family.Changed -= OnFamilyChanged;

    partial void OnKidNameChanged(string value)
    {
        if (!_isLoading)
        {
            _family.RenameKid(_family.ActiveKid, value);
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddWish))]
    private void AddWish()
    {
        var wish = _family.AddWish(NewWishText, SelectedSticker?.Key);
        if (wish is null)
        {
            return;
        }

        NewWishText = "";
        SelectedSticker = null;
        Load();
    }

    private bool CanAddWish() => !string.IsNullOrWhiteSpace(NewWishText) || SelectedSticker is not null;

    [RelayCommand]
    private void ChooseSticker(WishStickerViewModel sticker) =>
        SelectedSticker = SelectedSticker == sticker ? null : sticker;

    [RelayCommand]
    private void RemoveWish(WishItemViewModel item)
    {
        _family.RemoveWish(item.Wish);
        Load();
    }

    [RelayCommand]
    private void SendLetter()
    {
        if (_family.SendLetter(AdventCalendar.SeasonYear(_clock.Now)))
        {
            Load();
        }
    }

    [RelayCommand]
    private void WriteMore()
    {
        _family.ReopenLetter();
        Load();
    }

    [RelayCommand]
    private async Task ShareWithGrownUpsAsync()
    {
        if (await _gate.PassAsync())
        {
            var text = _family.ActiveKid.WishesAsText(sticker => _localizer[$"Sticker_{sticker}"]);
            await _share.ShareTextAsync(_localizer["ShareWishesTitle"], text);
        }
    }

    private void OnFamilyChanged(object? sender, EventArgs e)
    {
        if (_family.ActiveKid != _shownKid)
        {
            NewWishText = "";
            SelectedSticker = null;
            Load();
        }
        else
        {
            Salutation = _localizer[$"LetterDear_{_family.Settings.GiftBringer}"];
        }
    }

    private void Load()
    {
        _isLoading = true;
        var kid = _family.ActiveKid;
        _shownKid = kid;
        var bringer = _family.Settings.GiftBringer;

        Salutation = _localizer[$"LetterDear_{bringer}"];
        KidName = kid.Name;
        SentMessage = _localizer["LetterSentMessage", _localizer.BringerName(bringer)];

        Wishes.Clear();
        foreach (var wish in kid.Wishes)
        {
            Wishes.Add(new WishItemViewModel(wish, wish.Text.Length > 0 ? wish.Text : _localizer[$"Sticker_{wish.Sticker}"]));
        }

        HasWishes = Wishes.Count > 0;
        IsSent = kid.LetterSentYear == AdventCalendar.SeasonYear(_clock.Now) && HasWishes;
        _isLoading = false;
    }
}

public sealed partial record WishStickerViewModel(string Key, string Name);

public sealed class WishItemViewModel(Wish wish, string label)
{
    public Wish Wish { get; } = wish;

    public string Label { get; } = label;

    public string Sticker => Wish.Sticker ?? "star";
}
