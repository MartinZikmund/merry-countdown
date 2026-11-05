using System.Collections.ObjectModel;
using MerryCountdown.Core.Family;
using MerryCountdown.Core.Season;
using MerryCountdown.Core.Store;

namespace MerryCountdown.Core.ViewModels;

/// <summary>The grown-ups' corner: kids, the full version, and the things only a parent should touch.</summary>
public partial class ParentZoneViewModel : ViewModelBase
{
    private const string DefaultEventSticker = "cake";

    private readonly IFamilyService _family;
    private readonly IFullVersionService _fullVersion;
    private readonly IStringLocalizer _localizer;
    private readonly ISnackService _messages;
    private readonly IAppClock _clock;
    private bool _isLoading;

    public ParentZoneViewModel(
        IFamilyService family,
        IFullVersionService fullVersion,
        IStringLocalizer localizer,
        ISnackService messages,
        IAppClock clock)
    {
        _family = family;
        _fullVersion = fullVersion;
        _localizer = localizer;
        _messages = messages;
        _clock = clock;
        PageTitle = localizer["ParentZoneTitle"];
        NewEventDate = new DateTimeOffset(clock.Now.Date);
        Load();
    }

    public ObservableCollection<KidRowViewModel> Kids { get; } = [];

    public ObservableCollection<FamilyEventRowViewModel> Events { get; } = [];

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
            Kids.Add(new KidRowViewModel(kid, _family, canRemove));
        }

        MessageKid = Kids.FirstOrDefault(k => k.Kid == selectedKid) ?? Kids.FirstOrDefault();

        Events.Clear();
        foreach (var familyEvent in _family.Events)
        {
            Events.Add(new FamilyEventRowViewModel(familyEvent, new DateTime(2000, familyEvent.Month, familyEvent.Day).ToString("M", System.Globalization.CultureInfo.CurrentCulture)));
        }

        _isLoading = false;
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

public partial class KidRowViewModel(Kid kid, IFamilyService family, bool canRemove) : ObservableObject
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

    public override string ToString() => Kid.Name;
}

public sealed class FamilyEventRowViewModel(FamilyEvent familyEvent, string date)
{
    public FamilyEvent Event { get; } = familyEvent;

    public string Name => Event.Name;

    public string Date { get; } = date;

    public string Sticker => Event.Sticker;
}
