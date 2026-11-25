using System.Collections.ObjectModel;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Infrastructure;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Store;

namespace MerryCountdown.Core.ViewModels;

/// <summary>The grown-ups' corner: kids, the full version, and the things only a parent should touch.</summary>
public partial class ParentZoneViewModel : ViewModelBase
{
    private const string DefaultEventSticker = "cake";

    /// <summary>How long each photo stays up.</summary>
    public static readonly IReadOnlyList<int> PhotoIntervals = [30, 60, 300, 900, 1800, 3600];

    private const int CustomDateYear = 2028;

    private readonly IFamilyService _family;
    private readonly IFullVersionService _fullVersion;
    private readonly IStringLocalizer _localizer;
    private readonly ISnackService _messages;
    private readonly IAppClock _clock;
    private readonly IPhotoLibrary _photos;
    private bool _isLoading;

    public ParentZoneViewModel(
        IFamilyService family,
        IFullVersionService fullVersion,
        IStringLocalizer localizer,
        ISnackService messages,
        IAppClock clock,
        IPhotoLibrary photos)
    {
        _family = family;
        _fullVersion = fullVersion;
        _localizer = localizer;
        _messages = messages;
        _clock = clock;
        _photos = photos;
        BigDayOptions = [.. Enum.GetValues<BigDay>().Select(day => localizer.BigDayName(day))];
        BringerOptions = [.. Enum.GetValues<GiftBringer>().Select(bringer => localizer.BringerName(bringer))];
        PhotoIntervalOptions = [.. PhotoIntervals.Select(seconds => seconds < 60 ? localizer.Plural("PhotoIntervalSeconds", seconds) : localizer.Plural("PhotoIntervalMinutes", seconds / 60))];
        PageTitle = localizer["ParentZoneTitle"];
        NewEventDate = new DateTimeOffset(clock.Now.Date);
        Load();
    }

    /// <summary>In the order of <see cref="BigDay"/>, so an index is a value.</summary>
    public IReadOnlyList<string> BigDayOptions { get; }

    /// <summary>In the order of <see cref="GiftBringer"/>.</summary>
    public IReadOnlyList<string> BringerOptions { get; }

    public ObservableCollection<KidRowViewModel> Kids { get; } = [];

    public ObservableCollection<FamilyEventRowViewModel> Events { get; } = [];

    public ObservableCollection<PhotoRowViewModel> Photos { get; } = [];


    public IReadOnlyList<string> PhotoIntervalOptions { get; }

    public IReadOnlyList<int> DoorNumbers { get; } = Enumerable.Range(1, AdventCalendar.DoorCount).ToList();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFree))]
    public partial bool IsFull { get; set; }

    public bool IsFree => !IsFull;

    [ObservableProperty]
    public partial bool CanPurchase { get; set; }

    [ObservableProperty]
    public partial string BuyButtonText { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddKidCommand))]
    public partial string NewKidName { get; set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddEventCommand))]
    public partial string NewEventName { get; set; } = "";

    [ObservableProperty]
    public partial DateTimeOffset? NewEventDate { get; set; }

    [ObservableProperty]
    public partial KidRowViewModel? MessageKid { get; set; }

    [ObservableProperty]
    public partial int MessageDoor { get; set; } = 1;

    [ObservableProperty]
    public partial string MessageText { get; set; } = "";

    [ObservableProperty]
    public partial TimeSpan ArrivalTime { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomBigDay))]
    public partial int BigDayIndex { get; set; }

    public bool IsCustomBigDay => BigDayIndex == (int)BigDay.Custom;

    /// <summary>Only its month and day count. The year is a leap year so 29 February can be picked.</summary>
    [ObservableProperty]
    public partial DateTimeOffset CustomBigDate { get; set; }

    [ObservableProperty]
    public partial int BringerIndex { get; set; }

    [ObservableProperty]
    public partial bool UsesPhotos { get; set; }

    [ObservableProperty]
    public partial bool HasPhotos { get; set; }

    /// <summary>The grown-ups' question is skipped on this device.</summary>
    [ObservableProperty]
    public partial bool KeepUnlocked { get; set; }

    [ObservableProperty]
    public partial int PhotoIntervalIndex { get; set; }

    public override async void ViewLoaded()
    {
        _fullVersion.Changed += OnFullVersionChanged;
        Load();
        await LoadPriceAsync();
    }

    public override void ViewUnloaded() => _fullVersion.Changed -= OnFullVersionChanged;

    partial void OnArrivalTimeChanged(TimeSpan value)
    {
        if (!_isLoading && IsFull)
        {
            _family.Settings.ArrivalMinutes = (int)value.TotalMinutes;
            _family.SaveSettings();
        }
    }

    partial void OnBigDayIndexChanged(int value)
    {
        if (_isLoading || value < 0)
        {
            return;
        }

        _family.Settings.UseBigDay((BigDay)value);
        _family.SaveSettings();

        // Christmas Eve and Christmas Day each bring their own usual hour.
        _isLoading = true;
        ArrivalTime = TimeSpan.FromMinutes(_family.Settings.ArrivalMinutes);
        _isLoading = false;
    }

    partial void OnCustomBigDateChanged(DateTimeOffset value)
    {
        if (!_isLoading)
        {
            _family.Settings.CustomBigDayMonth = value.Month;
            _family.Settings.CustomBigDayDay = value.Day;
            _family.SaveSettings();
        }
    }

    partial void OnBringerIndexChanged(int value)
    {
        if (!_isLoading && value >= 0)
        {
            _family.Settings.GiftBringer = (GiftBringer)value;
            _family.SaveSettings();
        }
    }

    partial void OnUsesPhotosChanged(bool value) => SavePhotoSetting(s => s.UsesPhotos = value);

    partial void OnKeepUnlockedChanged(bool value)
    {
        if (!_isLoading)
        {
            _family.Settings.KeepGrownUpsUnlocked = value;
            _family.SaveSettings();
        }
    }

    partial void OnPhotoIntervalIndexChanged(int value)
    {
        if (value >= 0)
        {
            SavePhotoSetting(s => s.PhotoIntervalSeconds = PhotoIntervals[value]);
        }
    }

    partial void OnMessageKidChanged(KidRowViewModel? value) => LoadMessage();

    partial void OnMessageDoorChanged(int value) => LoadMessage();

    [RelayCommand]
    private async Task BuyAsync()
    {
        var result = await _fullVersion.PurchaseAsync();
        switch (result)
        {
            case PurchaseResult.Succeeded or PurchaseResult.AlreadyOwned:
                await _messages.ShowMessageAsync(_localizer["PurchaseThanksTitle"], _localizer["PurchaseThanksMessage"]);
                break;
            case PurchaseResult.Cancelled:
                break;
            default:
                await _messages.ShowMessageAsync(_localizer["PurchaseFailedTitle"], _localizer["PurchaseFailedMessage"]);
                break;
        }

        Load();
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        await _fullVersion.RefreshAsync();
        Load();
        if (!_fullVersion.IsFull)
        {
            await _messages.ShowMessageAsync(_localizer["RestoreNothingTitle"], _localizer["RestoreNothingMessage"]);
        }
    }

    [RelayCommand]
    private async Task AddPhotosAsync()
    {
        var result = await _photos.PickAndStoreAsync();
        if (result.Added.Count > 0)
        {
            _family.Settings.PhotoIds.AddRange(result.Added);
            _family.Settings.UsesPhotos = true;
            _family.SaveSettings();
            Load();
        }

        if (result.Failed > 0)
        {
            await _messages.ShowMessageAsync(_localizer["PhotoFailedTitle"], _localizer["PhotoFailedMessage"]);
        }
    }

    [RelayCommand]
    private async Task RemovePhotoAsync(PhotoRowViewModel row)
    {
        if (!await _messages.ConfirmAsync(_localizer["RemovePhotoTitle"], _localizer["RemovePhotoMessage"]))
        {
            return;
        }

        var settings = _family.Settings;
        settings.PhotoIds.Remove(row.Id);
        settings.UsesPhotos = settings.UsesPhotos && settings.PhotoIds.Count > 0;
        if (settings.PhotoIds.Count == 0 && settings.SceneId == SceneCatalog.PhotosId)
        {
            // The Photos scene has nothing left to show.
            settings.SceneId = SceneCatalog.All[0].Id;
        }

        _family.SaveSettings();
        _photos.Delete(row.Id);
        Load();
    }

    [RelayCommand(CanExecute = nameof(CanAddKid))]
    private void AddKid()
    {
        _family.AddKid(NewKidName);
        NewKidName = "";
        Load();
    }

    private bool CanAddKid() => !string.IsNullOrWhiteSpace(NewKidName);

    [RelayCommand]
    private async Task RemoveKidAsync(KidRowViewModel row)
    {
        var name = row.Name.Length > 0 ? row.Name : _localizer["KidNoName"];
        if (await _messages.ConfirmAsync(_localizer["RemoveKidTitle", name], _localizer["RemoveKidMessage"]))
        {
            _family.RemoveKid(row.Kid);
            Load();
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddEvent))]
    private void AddEvent()
    {
        if (NewEventDate is not { } date)
        {
            return;
        }

        _family.AddEvent(NewEventName, date.Month, date.Day, DefaultEventSticker);
        NewEventName = "";
        Load();
    }

    private bool CanAddEvent() => !string.IsNullOrWhiteSpace(NewEventName);

    [RelayCommand]
    private void RemoveEvent(FamilyEventRowViewModel row)
    {
        _family.RemoveEvent(row.Event);
        Load();
    }

    [RelayCommand]
    private void SaveMessage()
    {
        if (MessageKid is { } kid)
        {
            _family.SetDoorMessage(kid.Kid, MessageDoor, MessageText);
        }
    }

    private void OnFullVersionChanged(object? sender, EventArgs e) => Load();

    private void Load()
    {
        _isLoading = true;
        IsFull = _fullVersion.IsFull;
        CanPurchase = _fullVersion.CanPurchase;
        ArrivalTime = TimeSpan.FromMinutes(_family.Settings.ArrivalMinutes);

        var selectedKid = MessageKid?.Kid;
        Kids.Clear();
        var canRemove = _family.Kids.Count > 1;
        foreach (var kid in _family.Kids)
        {
            Kids.Add(new KidRowViewModel(kid, _family, canRemove, _localizer["KidNoName"]));
        }

        MessageKid = Kids.FirstOrDefault(k => k.Kid == selectedKid) ?? Kids.FirstOrDefault();

        var settings = _family.Settings;
        UsesPhotos = settings.UsesPhotos;
        KeepUnlocked = settings.KeepGrownUpsUnlocked;
        BigDayIndex = (int)settings.BigDay;
        CustomBigDate = new DateTimeOffset(new DateTime(CustomDateYear, settings.CustomBigDayMonth, settings.CustomBigDayDay));
        BringerIndex = (int)settings.GiftBringer;
        PhotoIntervalIndex = Math.Max(0, PhotoIntervals.ToList().IndexOf(settings.PhotoIntervalSeconds));
        Photos.Clear();
        foreach (var id in settings.PhotoIds)
        {
            Photos.Add(new PhotoRowViewModel(id, _photos.Load(id)));
        }

        HasPhotos = Photos.Count > 0;

        Events.Clear();
        foreach (var familyEvent in _family.Events)
        {
            Events.Add(new FamilyEventRowViewModel(familyEvent, new DateTime(2000, familyEvent.Month, familyEvent.Day).ToString("M", System.Globalization.CultureInfo.CurrentCulture)));
        }

        _isLoading = false;
    }

    private void SavePhotoSetting(Action<FamilySettings> change)
    {
        if (!_isLoading)
        {
            change(_family.Settings);
            _family.SaveSettings();
        }
    }

    private void LoadMessage()
    {
        MessageText = MessageKid is { } kid && kid.Kid.DoorMessages.TryGetValue(MessageDoor, out var message) ? message : "";
    }

    private async Task LoadPriceAsync()
    {
        var price = await _fullVersion.GetPriceAsync();
        BuyButtonText = price is null ? _localizer["BuyFullVersion"] : _localizer["BuyFullVersionPrice", price];
    }
}

public partial class KidRowViewModel(Kid kid, IFamilyService family, bool canRemove, string unnamed) : ObservableObject
{
    public Kid Kid { get; } = kid;

    public string Sticker => Kid.Sticker;

    public bool CanRemove { get; } = canRemove;

    public string Name
    {
        get => Kid.Name;
        set
        {
            if (Kid.Name != value)
            {
                family.RenameKid(Kid, value);
                OnPropertyChanged();
            }
        }
    }

    public string DisplayName => Kid.Name.Length > 0 ? Kid.Name : unnamed;

    public override string ToString() => DisplayName;
}

public sealed class PhotoRowViewModel(string id, byte[]? bytes)
{
    public string Id { get; } = id;

    public byte[]? Bytes { get; } = bytes;
}

public sealed class FamilyEventRowViewModel(FamilyEvent familyEvent, string date)
{
    public FamilyEvent Event { get; } = familyEvent;

    public string Name => Event.Name;

    public string Date { get; } = date;

    public string Sticker => Event.Sticker;
}
