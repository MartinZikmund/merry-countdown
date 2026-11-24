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
    private readonly ISoundService _sounds;
    private Kid? _shownKid;
    private bool _isLoading;
    private (Wish Wish, int Index)? _removed;

    public WishListViewModel(
        IFamilyService family,
        IAppClock clock,
        IStringLocalizer localizer,
        IParentGateService gate,
        IShareService share,
        KidPickerViewModel kidPicker,
        ISoundService sounds)
    {
        _sounds = sounds;
        _family = family;
        _clock = clock;
        _localizer = localizer;
        _gate = gate;
        _share = share;
        KidPicker = kidPicker;
        PageTitle = localizer["NavWishes"];

        foreach (var group in StickerCatalog.Groups)
        {
            WishStickerGroupViewModel groupViewModel = new(localizer[$"StickerGroup_{group.Key}"]);
            foreach (var key in group.Stickers)
            {
                WishStickerViewModel sticker = new(key, localizer[$"Sticker_{key}"]);
                groupViewModel.Stickers.Add(sticker);
                Stickers.Add(sticker);
            }

            StickerGroups.Add(groupViewModel);
        }

        Load();
    }

    public KidPickerViewModel KidPicker { get; }

    public int WishMaxLength => MaxWishLength;

    public ObservableCollection<WishItemViewModel> Wishes { get; } = [];

    /// <summary>Picture wishes, so kids who can't write yet can still ask for a puppy.</summary>
    public ObservableCollection<WishStickerViewModel> Stickers { get; } = [];

    public IList<WishStickerGroupViewModel> StickerGroups { get; } = [];

    [ObservableProperty]
    public partial string Salutation { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend), nameof(SendHint))]
    [NotifyCanExecuteChangedFor(nameof(SendLetterCommand))]
    public partial string KidName { get; set; } = "";

    [ObservableProperty]
    public partial string SentMessage { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddWishCommand))]
    public partial string NewWishText { get; set; } = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWriting))]
    public partial bool IsSent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend), nameof(SendHint), nameof(IsEmpty))]
    [NotifyCanExecuteChangedFor(nameof(SendLetterCommand))]
    public partial bool HasWishes { get; set; }

    /// <summary>The line that has just landed on the letter, for the page to scroll to and show off.</summary>
    [ObservableProperty]
    public partial WishItemViewModel? ArrivedWish { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoRemoveCommand))]
    public partial bool CanUndoRemove { get; set; }

    [ObservableProperty]
    public partial string RemovedMessage { get; set; } = "";

    [ObservableProperty]
    public partial bool CalmMotion { get; set; }

    public bool IsWriting => !IsSent;

    public bool IsEmpty => !HasWishes;

    public bool CanSend => HasWishes && !string.IsNullOrWhiteSpace(KidName);

    /// <summary>What still stands between the kid and sending, or a nudge to send.</summary>
    public string SendHint => _localizer[!HasWishes ? "SendNeedsWish" : CanSend ? "SendReady" : "SendNeedsName"];

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
    private void AddWish() => Add(null);

    private bool CanAddWish() => !string.IsNullOrWhiteSpace(NewWishText);

    /// <summary>Picking a picture is the whole job for a kid who can't write, so it adds the wish straight away.</summary>
    [RelayCommand]
    private void AddPictureWish(WishStickerViewModel sticker)
    {
        _sounds.Play(SoundEffect.Sticker);
        Add(sticker.Key);
    }

    private void Add(string? sticker)
    {
        var wish = _family.AddWish(NewWishText, sticker);
        if (wish is null)
        {
            return;
        }

        NewWishText = "";
        ForgetRemoved();
        Load();
        ArrivedWish = Wishes.FirstOrDefault(w => w.Wish == wish);
    }

    [RelayCommand]
    private void RemoveWish(WishItemViewModel item)
    {
        var index = _family.ActiveKid.Wishes.IndexOf(item.Wish);
        _family.RemoveWish(item.Wish);
        _removed = (item.Wish, index);
        RemovedMessage = _localizer["WishRemoved", item.Label];
        CanUndoRemove = true;
        Load();
    }

    [RelayCommand(CanExecute = nameof(CanUndoRemove))]
    private void UndoRemove()
    {
        if (_removed is not { } removed)
        {
            return;
        }

        _family.RestoreWish(removed.Wish, removed.Index);
        ForgetRemoved();
        Load();
        ArrivedWish = Wishes.FirstOrDefault(w => w.Wish == removed.Wish);
    }

    private void ForgetRemoved()
    {
        _removed = null;
        CanUndoRemove = false;
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private void SendLetter()
    {
        if (CanSend && _family.SendLetter(AdventCalendar.SeasonYear(_clock.Now)))
        {
            _sounds.Play(SoundEffect.Letter);
            ForgetRemoved();
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
            ForgetRemoved();
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
        CalmMotion = _family.Settings.CalmMotion;
        ArrivedWish = null;
        KidName = kid.Name;
        SentMessage = _localizer[$"LetterSent_{bringer}"];

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

public sealed partial class WishStickerViewModel(string key, string name) : ObservableObject
{
    public string Key { get; } = key;

    public string Name { get; } = name;

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}

public sealed class WishStickerGroupViewModel(string name)
{
    public string Name { get; } = name;

    public IList<WishStickerViewModel> Stickers { get; } = [];
}

public sealed class WishItemViewModel(Wish wish, string label)
{
    public Wish Wish { get; } = wish;

    public string Label { get; } = label;

    public string Sticker => Wish.Sticker ?? "star";
}
